using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CaseExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace GenerateTestRequestObjects
{
	public static class Parsers
	{
		private const string disclaimer = @"/*
 * This file is automatically generated by the GenerateTestRequestObjects
 * project in the Utilities folder. Do not edit this file directly as
 * its contents may be overwritten at any point.
 *
 * Should a class here need to be altered for any reason, you should look
 * first to the auto generation program for possible updating to include
 * the needed special case. Otherwise, it is possible to create your own
 * manually written ApiRequest object, though this is generally discouraged.
 */
";

		public static (bool, string, string, string) GetMethodNameAndAuthorization(
				MethodDeclarationSyntax method
				, string                controllerName)
		{
			var validPathAttrs = new List<string>
			{
					"HttpGet"
					, "HttpPost"
					, "HttpPut"
					, "HttpDelete"
					,
			};

			var httpRequestType = "";
			var httpPath = "";
			var formattedPath = "";
			var anonymousAllowed = false;

			var attributes =
					method.AttributeLists.SelectMany(attributeList => attributeList.Attributes);

			// Reject any controller endpoints with complex routing
			if (attributes.Count(
						x => (x.Name.ToString()
							   .IndexOf("Route", StringComparison.CurrentCultureIgnoreCase)
							  >= 0)
							 || (x.Name.ToString()
								  .IndexOf("Http", StringComparison.CurrentCultureIgnoreCase)
								 >= 0))
				> 1)
			{
				throw new Exception(
						$"The SQE API only supports endpoints with a single route path attribute. The method {method.Identifier.Text} violates this policy.");
			}

			foreach (var attribute in attributes)
			{
				if (attribute.Name.ToString() == "AllowAnonymous")
					anonymousAllowed = true;
				else
				{
					if (attribute.Name.ToString() == "ApiExplorerSettings")
						continue;

					var attributeName = attribute.Name.ToString();

					if (!validPathAttrs.Contains(
							attributeName
							, StringComparer.CurrentCultureIgnoreCase))
					{
						throw new Exception(
								$"The SQE API does not support the attribute {attributeName}.");
					}

					httpRequestType = attributeName.Replace("Http", "");

					foreach (var argument in attribute.ArgumentList.Arguments)
					{
						httpPath = argument.ToString();

						formattedPath = string.Join(
								""
								, httpPath.Replace("[controller]", controllerName)
										  .Replace("[action]", method.Identifier.Text)
										  .Replace("{", "")
										  .Replace("}", "")
										  .Replace("\"", "")
										  .Split("/")
										  .Select(x => x.ToPascalCase()));

						// Reject any routes using token replacement with tokens other than [controller] or [action].
						if (formattedPath.Contains("[")
							|| formattedPath.Contains("]"))
						{
							throw new Exception(
									$"The SQE API only allows paths using token replacement with the [controller] and [action] tokens. The method {method.Identifier.Text} violates this policy.");
						}
					}
				}
			}

			return (anonymousAllowed, $"{httpRequestType}{formattedPath}", httpRequestType
					, httpPath);
		}

		/// <summary>
		///  Provided a ClassDeclarationSyntax node and all necessary project syntax and semantics,
		///  parse and write the methods of the controller class to an ApiRequest object for
		///  integration tests.
		/// </summary>
		/// <param name="testFolder"></param>
		/// <param name="node"></param>
		/// <param name="semModels"></param>
		/// <param name="serviceMethods"></param>
		/// <param name="project"></param>
		/// <returns></returns>
		public static async Task<List<ParameterDescription>> ParseAndWriteClassesAsync(
				string                                                    testFolder
				, ClassDeclarationSyntax                                  node
				, IEnumerable<SemanticModel>                              semModels
				, IEnumerable<MethodDescription<MethodDeclarationSyntax>> serviceMethods
				, IEnumerable<MethodDeclarationSyntax>                    broadcastMethods
				, Project                                                 project)
		{
			var controllerName = node.Identifier.ValueText.ToLowerInvariant()
									 .Replace("controller", "");

			Console.WriteLine(
					$"Writing to {Path.Combine(testFolder, "ApiRequests", $"{controllerName.ToPascalCase()}Requests.cs")}");

			using (var outputFile = new StreamWriter(
					Path.Combine(
							testFolder
							, "ApiRequests"
							, $"{controllerName.ToPascalCase()}Requests.cs")))
			{
				// Begin writing file
				await outputFile.WriteLineAsync(disclaimer);

				await outputFile.WriteLineAsync(
						@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
");

				var parsedControllerMethods = new ApiRequestsDescription();

				// Look at each method in the controller class
				foreach (var method in node.DescendantNodes().OfType<MethodDeclarationSyntax>())
				{
					var methodDescription = await ParseMethodsAsync(
							node
							, semModels
							, serviceMethods
							, broadcastMethods
							, project
							, method
							, controllerName);

					if (!parsedControllerMethods.requests.ContainsKey(methodDescription.RequestType)
					)
					{
						parsedControllerMethods.requests.Add(
								methodDescription.RequestType
								, new List<ApiRequestEndpointDescription>());
					}

					if (parsedControllerMethods.requests.TryGetValue(
							methodDescription.RequestType
							, out var req))
						req.Add(methodDescription);
				}

				await Writers.WriteEndpointsAsync(parsedControllerMethods, outputFile);

				// Write ending to the file
				await outputFile.WriteLineAsync("\n}");

				return parsedControllerMethods.requests
											  .SelectMany(x => x.Value.SelectMany(y => y.listeners))
											  .ToList();
			}
		}

		/// <summary>
		///  Collect the details of a method in a controller class
		///  so it can be used to write an ApiRequest object.
		/// </summary>
		/// <param name="testFolder"></param>
		/// <param name="node"></param>
		/// <param name="semModels"></param>
		/// <param name="serviceMethods"></param>
		/// <param name="project"></param>
		/// <param name="method"></param>
		/// <param name="controllerName"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private static async Task<ApiRequestEndpointDescription> ParseMethodsAsync(
				ClassDeclarationSyntax                                    node
				, IEnumerable<SemanticModel>                              semModels
				, IEnumerable<MethodDescription<MethodDeclarationSyntax>> serviceMethods
				, IEnumerable<MethodDeclarationSyntax>                    broadcastMethods
				, Project                                                 project
				, MethodDeclarationSyntax                                 method
				, string                                                  controllerName)
		{
			var methodDescription = new ApiRequestEndpointDescription
			{
					comments = method.GetLeadingTrivia().ToString().Trim(),
			};

			// Get the return type
			var oType = method.ReturnType.ToString();
			const string pat = @"([a-zA-Z]+)>";

			// Instantiate the regular expression object.
			var r = new Regex(pat, RegexOptions.IgnoreCase);

			// Match the regular expression pattern against a text string.
			var m = r.Match(oType);

			while (m.Success)
			{
				oType = m.Groups[1].ToString();
				m = m.NextMatch();
			}

			methodDescription.OType = oType == "ActionResult"
					? "EmptyOutput"
					: oType;

			// Format the method name and grab authorization
			var (_, _, httpRequestType, httpPath) =
					GetMethodNameAndAuthorization(method, controllerName);

			methodDescription.HttpString = httpPath.Replace("[controller]", controllerName);

			// Format the method parameters
			foreach (var param in method.ParameterList.Parameters)
			{
				if (param.AttributeLists.ToString().Contains("FromBody")
					&& (methodDescription.bodyParams == null))
				{
					methodDescription.bodyParams = new ParameterDescription(
							param.Type.ToString()
							, "payload");

					methodDescription.IType = param.Type.ToString();
				}

				if (param.AttributeLists.ToString().Contains("FromRoute"))
				{
					methodDescription.routeParams.Add(
							new ParameterDescription(
									param.Type.ToString()
									, param.Identifier.ToString()));
				}

				if (param.AttributeLists.ToString().Contains("FromQuery"))
				{
					methodDescription.queryParams.Add(
							new ParameterDescription(
									param.Type.ToString()
									, param.Identifier.ToString()));
				}
			}

			methodDescription.RequestType = httpRequestType.ToLowerInvariant() switch
											{
													"get"      => HttpMethod.Get
													, "post"   => HttpMethod.Post
													, "put"    => HttpMethod.Put
													, "delete" => HttpMethod.Delete
													, _ => throw new Exception(
															$"Unsupported http verb: {httpRequestType}")
													,
											};

			// Get the first invoked expression
			// The rigid design practice dictates that it will be the call to the service
			var invokedMethod = method.DescendantNodes()
									  .OfType<InvocationExpressionSyntax>()
									  .FirstOrDefault();

			var foundInvokedMethod = false;

			// Start searching the semantic model for this expression
			foreach (var semModel in semModels)
			{
				try
				{
					// Get the symbol, which will refer to the interface
					var invokedSymbol = semModel.GetSymbolInfo(invokedMethod).Symbol;

					if (invokedSymbol == null)
						continue;

					// Get the actual implementation of the interface
					var implementations =
							await SymbolFinder.FindImplementationsAsync(
									invokedSymbol
									, project.Solution);

					invokedSymbol = implementations.First();
					foundInvokedMethod = true;

					// Get the MethodDeclarationSyntax for the called expression
					// (so we can inspect its body for SignalR broadcasts
					var invokedSyntax =
							serviceMethods.FirstOrDefault(
									x => x.Symbol == invokedSymbol.ToString());

					var body = invokedSyntax.Syntax.Body.ToString();

					const string broadcastRx =
							@"\.Clients\s*?\.(?:.|\n)*?\)\s*?\.([a-zA-Z]+)\((?:.|\n)*?\)\s?;";

					// Instantiate the regular expression object.
					var rx = new Regex(broadcastRx, RegexOptions.Multiline);

					// Match the regular expression pattern against a text string.
					var match = rx.Match(body);

					// We capture the last broadcast method (the current test system can only handle a single listener)
					while (match.Success)
					{
						var listenerName = match.Groups[1].ToString();

						// If there is a broadcast, find its type
						if (!string.IsNullOrEmpty(listenerName))
						{
							var broadcastMethodDescription = broadcastMethods.First(
									x => x.Identifier.ToString() == listenerName);

							var listenDescription = new ParameterDescription(
									broadcastMethodDescription.ParameterList.Parameters.First()
															  .Type.ToString()
									, listenerName);

							// Insert the listener into the method description if is does not already exist
							if (!methodDescription.listeners.Any(
									x => (x.ParamName == listenDescription.ParamName)
										 && (x.ParamType == x.ParamType)))
							{
								methodDescription.listeners.Add(
										new ParameterDescription(
												broadcastMethodDescription.ParameterList.Parameters
																		  .First()
																		  .Type.ToString()
												, listenerName));
							}
						}

						match = match.NextMatch();
					}
				}
				catch { }
			}

			if (!foundInvokedMethod)
			{
				throw new Exception(
						$@"Could not find a method declaration for {
									invokedMethod.ToString()
								},
called in method {
									method.ToString()
								} of class {
									node.ToString()
								}");
			}

			return methodDescription;
		}

		/// <summary>
		///  Given the compilation and the semantic models, return the MethodDeclarationSyntax and the symbol name
		///  for all methods in the given namespaces
		/// </summary>
		/// <param name="compilation"></param>
		/// <param name="semModels"></param>
		/// <returns></returns>
		public static async Task<IEnumerable<MethodDescription<TSyntax>>>
				GetAllSyntaxNodesAsync<TSyntax>(
						Compilation                  compilation
						, IEnumerable<SemanticModel> semModels
						, List<string>               namespaces)
				where TSyntax : SyntaxNode
		{
			return (await compilation.SyntaxTrees.SelectManyAsync(
					async x =>
					{
						var results = (await x.GetRootAsync()).DescendantNodes()
															  .OfType<TSyntax>()
															  .Where(
																	  x =>
																	  {
																		  NamespaceDeclarationSyntax
																				  namespaceDeclarationSyntax
																						  = null;

																		  if (!Helpers
																			   .SyntaxNodeHelper
																			   .TryGetParentSyntax(
																					   x
																					   , out
																					   namespaceDeclarationSyntax)
																		  )
																			  return false;

																		  return namespaces
																				  .Contains(
																						  namespaceDeclarationSyntax
																								  .Name
																								  .ToString());
																	  });

						return results;
					})).Select(
					x =>
					{
						foreach (var semModel in semModels)
						{
							try
							{
								var invokedSymbol = semModel.GetDeclaredSymbol(x);

								if (invokedSymbol == null)
									continue;

								return new MethodDescription<TSyntax>(invokedSymbol.ToString(), x);
							}
							catch // Do nothing on an error, the symbol is probably in another semantic model
							{ }
						}

						return new MethodDescription<TSyntax>(default, x);
					});
		}
	}
}
