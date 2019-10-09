using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CaseExtensions;

namespace sqe_realtime_hub_builder
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			ParseSqeHttpControllers();
		}

		private static void ParseSqeHttpControllers()
		{
			var folder = $"{AppDomain.CurrentDomain.BaseDirectory}../../../../sqe-http-server/Controllers";
			var projectRoot = $"{AppDomain.CurrentDomain.BaseDirectory}../../../";
			var d = new DirectoryInfo(folder);//Assuming Test is your Folder
			var files = d.GetFiles("*.cs"); //Getting Text files
			var classFields = new List<ControllerField>();
			foreach(var file in files )
			{
				var filepath = $"{folder}/{file.Name}";
				var controllerName = file.Name.Replace("Controller.cs", "");
				Console.WriteLine($"Parsing {filepath}...");
				var code = new StreamReader(filepath).ReadToEnd();
				var tree = CSharpSyntaxTree.ParseText(code);
				var root = tree.GetCompilationUnitRoot();
				using (var outputFile = new StreamWriter($"{projectRoot}/{controllerName}Hub.cs.txt"))
				{
					outputFile.Write(_autogenFileDisclaimer);
					foreach (var element in root.Usings.Where(element => element.Name.ToString() != "Microsoft.AspNetCore.Mvc"))
						outputFile.WriteLine($"using {element.Name};");
					outputFile.WriteLine("using Microsoft.AspNetCore.SignalR;\n");
					outputFile.WriteLine(_hubClassTemplate);

					// Get relevant members of root
					var members = root.DescendantNodes().OfType<MemberDeclarationSyntax>().ToList();

					classFields = classFields.Union(AnalyzeController(members), new FieldComparer()).ToList();
					
					foreach (var member in members)
					{
						switch (member)
						{
							case MethodDeclarationSyntax method:
								// Copy any method comments
								var comments = method.GetLeadingTrivia().ToString();
								if (!string.IsNullOrEmpty(comments))
									outputFile.WriteLine(comments);
								
								// Format the method modifiers
								var methodModifiers = string.Join(" ", method.Modifiers.Select(x => x.Text));
								
								// Format the return type
								const string actionResultString = "<ActionResult";
								var returnType = method.ReturnType.ToString().Contains(actionResultString)
									? Regex.Replace(method.ReturnType.ToString().Replace(actionResultString, ""),
										">$","")
									: method.ReturnType.ToString();
								
								// Format the method name and grab authorization
								var httpRequestType = "";
								var httpPath = "";
								var anonymousAllowed = false; 
								foreach (var attribute in method.AttributeLists.SelectMany(attributeList => attributeList.Attributes))
								{
									if (attribute.Name.ToString() == "AllowAnonymous")
										anonymousAllowed = true;
									else
									{
										httpRequestType = attribute.Name.ToString().Replace("Http", "");
										foreach (var argument in attribute.ArgumentList.Arguments)
										{
											httpPath = string.Join("", 
												argument.ToString().Replace("[controller]", controllerName)
													.Replace("{","")
													.Replace("}","")
													.Replace("\"","")
													.Split("/")
													.Select(x => x.ToPascalCase()));
										}
									}
								}

								// Format the method parameters
								var methodParams = string.Join(", ", 
									method.ParameterList.Parameters.Select(x => $"{x.Type} {x.Identifier}"));
								
								// Write the method signature
								outputFile.WriteLine(anonymousAllowed ? "[AllowAnonymous]" : "[Authorize]");

								var methodSignature = $"{methodModifiers} {returnType} {httpRequestType}{httpPath}({methodParams})";
								outputFile.WriteLine(methodSignature);

								var methodBody = Regex.Replace(method.Body.ToString()
									.Replace("\n", "")
									.Replace("\t", ""),
									@"\s*\)\s*;", httpRequestType == "Get" ? ");" : ", clientId: Context.ConnectionId);");
								outputFile.WriteLine(methodBody);
								outputFile.WriteLine("");
								break;
						}
					}
					// Close out the file
					outputFile.WriteLine("\t}\n}");
				}
			}

			WriteHubController(classFields, projectRoot);
		}

		private static List<ControllerField> AnalyzeController(List<MemberDeclarationSyntax> elements)
		{
			var cls = elements.OfType<ClassDeclarationSyntax>().ToList();
			// Check for more than one class (there should only ever be one).
			if (cls.Count() != 1)
				Console.WriteLine("More than one class in controller"); // TODO: throw error
			var controllerClass = cls.First();
			
			var fields = ParseControllerFields(controllerClass.Members.OfType<FieldDeclarationSyntax>().ToList());

			var constructors = controllerClass.Members.OfType<ConstructorDeclarationSyntax>().ToList();
			// Check for more than one constructor (there should only ever be one).
			if (constructors.Count() != 1)
				Console.WriteLine("More than one constructor in class"); // TODO: throw error
			var constructor = constructors.First();
			
			if (!ValidateControllerConstructor(constructor, fields))
				Console.WriteLine("Constructor and class fields do not match."); // TODO, just run the method and throw errors
			
			return fields;
		}

		private static List<ControllerField> ParseControllerFields(List<FieldDeclarationSyntax> fields)
		{
			var controllerFields = new List<ControllerField>();
			foreach (var field in fields)
			{
				var modifiers = string.Join(" ", field.Modifiers.Select(x => x.Value));
				var name = field.Declaration.Variables.First().ToString();
				var type = field.Declaration.Type.ToString();
				controllerFields.Add(new ControllerField(modifiers, name, type));
			}
			return controllerFields;
		}
		
		private static bool ValidateControllerConstructor(ConstructorDeclarationSyntax cst, List<ControllerField> fields)
		{
			var tempFieldList = new List<ControllerField>(fields);
			var paramMatchNotFound = false;
			foreach (var param in cst.ParameterList.Parameters)
			{
				var type = param.Type.ToString();
				var name = param.Identifier.Value;
				
				var r = new Regex($@"\s*([A-z0-9_]+)\s*=\s*{name}\s*;");
				var paramMatches = r.Match(cst.Body.ToString());
				var assignedTo = paramMatches.Success && paramMatches.Groups.Count() >= 2 
					? paramMatches.Groups[1].ToString() : "";
				
				paramMatchNotFound = tempFieldList.Count(x => x.type == type && x.name == assignedTo) != 1;
				tempFieldList = tempFieldList.Where(x => x.type != type && x.name != assignedTo).ToList();
			}

			return !paramMatchNotFound && !tempFieldList.Any();
		}

		private static void WriteHubController(List<ControllerField> fields, string projectRoot)
		{
			var template = new StreamReader($"{projectRoot}/HubConstructorTemplate.txt").ReadToEnd();
			template = template
				.Replace("$Fields", 
					string.Join("\n", fields.Select(x => $"\t\t{x.modifiers} {x.type} {x.name};")))
				.Replace("$Params", string.Join(", ", fields.Select(x => $"{x.type} {x.name.Replace("_", "")}")))
				.Replace("$Body", 
					string.Join("\n", fields.Select(x => $"\t\t\t{x.name} = {x.name.Replace("_", "")};")));
			using (var outputFile = new StreamWriter($"{projectRoot}/HubConstructor.cs.txt"))
			{
				outputFile.Write(template);
			}
		}

		private class ControllerField
		{
			public string modifiers { get; set; }
			public string name { get; set; }
			public string type { get; set; }
			public ControllerField(string modifiers, string name, string type)
			{
				this.modifiers = modifiers;
				this.name = name;
				this.type = type;
			}
		} 
		
		private class FieldComparer : IEqualityComparer<ControllerField>
		{
			public bool Equals(ControllerField x, ControllerField y)
			{
				return x != null && y != null 
				                 && x.modifiers.Equals(y.modifiers)
				                 && x.name.Equals(y.name)
				                 && x.type.Equals(y.type);
			}

			public int GetHashCode(ControllerField obj)
			{
				return 0;
			}
		}

		private const string _hubClassTemplate = @"namespace SQE.API.Realtime.Hubs
{
    public partial class MainHub : Hub
    {";

		private const string _autogenFileDisclaimer = @"/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-http-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

";
	}
}