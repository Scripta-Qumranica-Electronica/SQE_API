/*
 * This program reads in all the HTTP controller files in the HttpController folder
 * of the `sqe-api-server` project. It parses them using Roslyn and automatically builds
 * corresponding SignalR real time hub methods from them.
 * 
 * The realtime hub is created as a partial class, which allows each HTTP Controller file
 * to have a corresponding Hub in the RealtimeHubs folder of the `sqe-api-server` project.
 * The constructor for this partial class is generated based on the template in the file
 * HubConstructorTemplate.txt at the root of this project. Additional hub methods related
 * to hub subscription are copied from SubsciptionHub.cs.txt at the root of this project.
 *
 * Any further custom hub methods should be implemented via this program and never
 * directly in the `sqe-api-server` project.
 */

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
            // TODO: start by deleting all files in the RealtimeHubs folder.

            // TODO: Can we find a better way to resolve these paths instead of all the backtracking?
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../", "../", "../", "../", "sqe-api-server", "HttpControllers");
            var projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../", "../", "../");
            var dir = new DirectoryInfo(folder);
            var files = dir.GetFiles("*.cs");
            var classFields = new List<ControllerField>();

            // Parse each comtroller file
            foreach (var file in files)
            {
                classFields = classFields.Union(ParseSqeHttpControllers(projectRoot, file), new FieldComparer()).ToList();
            }

            // Format and write the Hub controller
            WriteHubController(classFields, projectRoot);

            // Format and write the custom subscription controller
            CopySubscriptionHubController(projectRoot);
        }

        private static List<ControllerField> ParseSqeHttpControllers(string projectRoot, FileInfo file)
        {
            /*
             * TODO: We can only do this efficiently if the full endpoint path is set before each controller method.
             * We need a way to check that endpoint paths are not being build with nested directives and then throw
             * an error if such instances are found.
             */

            var controllerName = file.Name.Replace("Controller.cs", "");
            Console.WriteLine($"Parsing {file.FullName}");

            // Parse the code to the relevant members
            var code = new StreamReader(file.FullName).ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var members = root.DescendantNodes().OfType<MemberDeclarationSyntax>().ToList();

            // Begin writing analyzed controller to Hub
            using (var outputFile = new StreamWriter(Path.Combine(projectRoot, "../", "sqe-api-server", "RealtimeHubs", $"{controllerName}Hub.cs")))
            {
                // Write disclaimer to file
                outputFile.Write(_autogenFileDisclaimer);

                // Collect and write using statements
                foreach (var element in root.Usings.Where(element => element.Name.ToString() != "Microsoft.AspNetCore.Mvc"))
                    outputFile.WriteLine($"using {element.Name};");
                outputFile.WriteLine("using Microsoft.AspNetCore.SignalR;\n");

                // Write hub class declaration
                outputFile.WriteLine(_hubClassTemplate);

                // Parse individual methods
                foreach (var method in members.OfType<MethodDeclarationSyntax>().ToList())
                {
                    // Copy the comments
                    WriteMethodCommentsToFile(method, outputFile);

                    // Format the method modifiers
                    var methodModifiers = string.Join(" ", method.Modifiers.Select(x => x.Text));

                    // Format the return type
                    var returnType = ProcessReturnType(method);

                    // Format the method name and grab authorization
                    var (anonymousAllowed, methodName, httpRequestType) = GetMethodNameAndAuthorization(method, controllerName);

                    // Format the method parameters
                    var methodParams = string.Join(", ",
                        method.ParameterList.Parameters.Select(x => $"{x.Type} {x.Identifier}"));

                    // Format the method signature from method modifiers, return type, name, and parameters
                    var methodSignature = $"{methodModifiers} {returnType} {methodName}({methodParams})";

                    // Write the method signature
                    outputFile.WriteLine(anonymousAllowed ? "[AllowAnonymous]" : "[Authorize]");
                    outputFile.WriteLine(methodSignature);

                    // Format the method body and add clientId to mutate requests
                    var methodBody = Regex.Replace(method.Body.ToString()
                        .Replace("\n", "")
                        .Replace("\t", "")
                        .Replace("{ ", "{\n")
                        .Replace(" }", "\n}")
                        .Replace("return ", returnType == "Task" ? "" : "return "),
                        @"\s*\)\s*;", httpRequestType == "Get" ? ");" : ", clientId: Context.ConnectionId);");

                    // Write the method body
                    outputFile.WriteLine(methodBody);
                    outputFile.WriteLine("");
                }
                // Write ending to the file
                outputFile.WriteLine("\t}\n}");
            }

            // return info for any dependency injected fields
            return AnalyzeController(members);
        }

        private static void WriteMethodCommentsToFile(MethodDeclarationSyntax method, StreamWriter outputFile)
        {
            var comments = method.GetLeadingTrivia().ToString().Trim();
            if (!string.IsNullOrEmpty(comments))
                outputFile.WriteLine(comments);
        }

        private static string ProcessReturnType(MethodDeclarationSyntax method)
        {
            const string actionResultString = "<ActionResult";
            return method.ReturnType.ToString().Contains(actionResultString)
                ? Regex.Replace(method.ReturnType.ToString().Replace(actionResultString, ""),
                    ">$", "")
                : method.ReturnType.ToString();
        }

        private static (bool, string, string) GetMethodNameAndAuthorization(MethodDeclarationSyntax method, string controllerName)
        {
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
                                .Replace("{", "")
                                .Replace("}", "")
                                .Replace("\"", "")
                                .Split("/")
                                .Select(x => x.ToPascalCase()));
                    }
                }
            }

            return (anonymousAllowed, $"{httpRequestType}{httpPath}", httpRequestType);
        }

        private static List<ControllerField> AnalyzeController(List<MemberDeclarationSyntax> elements)
        {
            var cls = elements.OfType<ClassDeclarationSyntax>().ToList();
            // Check for more than one class (there should only ever be one).
            if (cls.Count != 1)
                throw new Exception("Error: More than one class in controller");
            var controllerClass = cls.First();

            var fields = ParseControllerFields(controllerClass.Members.OfType<FieldDeclarationSyntax>().ToList());

            var constructors = controllerClass.Members.OfType<ConstructorDeclarationSyntax>().ToList();
            // Check for more than one constructor (there should only ever be one).
            if (constructors.Count != 1)
                throw new Exception("Error: More than one constructor in class");
            var constructor = constructors.First();

            ValidateControllerConstructor(constructor, fields);
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

        private static void ValidateControllerConstructor(ConstructorDeclarationSyntax cst, List<ControllerField> fields)
        {
            var tempFieldList = new List<ControllerField>(fields);
            var paramMatchNotFound = false;
            foreach (var param in cst.ParameterList.Parameters)
            {
                var type = param.Type.ToString();
                var name = param.Identifier.Value;

                var r = new Regex($@"\s*([A-z0-9_]+)\s*=\s*{name}\s*;");
                var paramMatches = r.Match(cst.Body.ToString());
                var assignedTo = paramMatches.Success && paramMatches.Groups.Count >= 2
                    ? paramMatches.Groups[1].ToString() : "";

                paramMatchNotFound = tempFieldList.Count(x => x.type == type && x.name == assignedTo) != 1;
                tempFieldList = tempFieldList.Where(x => x.type != type && x.name != assignedTo).ToList();
            }

            if (paramMatchNotFound || tempFieldList.Any())
                throw new Exception("Error: The constructor for this controller does not set a value to every instance variable.");
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
            using (var outputFile = new StreamWriter($"{projectRoot}/../sqe-api-server/RealtimeHubs/HubConstructor.cs"))
            {
                outputFile.Write(_autogenFileDisclaimer);
                outputFile.Write(template);
            }
        }

        private static void CopySubscriptionHubController(string projectRoot)
        {
            var subscriptionHub = new StreamReader(Path.Combine(projectRoot, "SubscriptionHub.cs.txt")).ReadToEnd();
            using (var outputFile = new StreamWriter(Path.Combine(projectRoot, "../", "sqe-api-server", "RealtimeHubs", "SubscriptionHub.cs")))
            {
                outputFile.Write(_subscriptionHubDisclaimer);
                outputFile.Write(subscriptionHub);
            }
        }

        private class ControllerField
        {
            public string modifiers { get; }
            public string name { get; }
            public string type { get; }
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

        private const string _hubClassTemplate = @"namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub
    {";

        private const string _autogenFileDisclaimer = @"/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

";

        private const string _subscriptionHubDisclaimer = @"/*
 * This file is autogenerated from (solution root)/sqe-realtime-hub-builder/SubscriptionHub.cs.txt
 * Please edit that file if any changes need to be made.
 */

";
    }
}