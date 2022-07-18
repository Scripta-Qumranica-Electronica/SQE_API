/*
 * This program reads in all the HTTP controller files in the HttpController namespace
 * of the `sqe-api-server` project. It parses them using Roslyn and automatically builds
 * corresponding ApiRequest objects from them for use in the integration tests.
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerateTestRequestObjects
{
	internal static class Program
	{
		private static readonly Regex _rx = new Regex(
				@"Task?<(?<return>.*)?>"
				, RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static async Task Main(string[] args)
		{
			//MSBuildLocator.RegisterDefaults();
			await ParseSqeHttpControllers();
		}

		private static async Task ParseSqeHttpControllers()
		{
			Console.WriteLine(
					"Parsing the HTTP controllers and creating corresponding ApiRequest Objects.");

			// TODO: Can we find a better way to resolve these paths instead of all the backtracking?
			var projectRoot = Path.GetFullPath(
					Path.Combine(
							AppDomain.CurrentDomain.BaseDirectory
							, "../"
							, "../"
							, "../"
							, "../"));

			var ApiServerRoot =
					Path.GetFullPath(Path.Combine(projectRoot, "../", "sqe-api-server"));

			var csProjFile = Path.Combine(ApiServerRoot, "sqe-api-server.csproj");

			var testFolder = Path.Combine(ApiServerRoot, "../", "sqe-api-test");

			// Get the compilation of the project
			Console.WriteLine($"Parsing {csProjFile}");
			var manager = new AnalyzerManager();
			var analyzer = manager.GetProject(csProjFile);
			var workspace = new AdhocWorkspace();
			var project = analyzer.AddToWorkspace(workspace);
			var compilation = await project.GetCompilationAsync();

			// Fetch the Semantic models for only the controllers and service.
			// We want failures if things are not found in these two namespaces.
			var semModels = compilation.SyntaxTrees.Select(x => compilation.GetSemanticModel(x))

									   // TODO: really get the namespace
									   .Where(
											   x => x.SyntaxTree.GetRoot()
													 .ToString()
													 .Contains("namespace SQE.API.Server.Services")
													|| x.SyntaxTree.GetRoot()
														.ToString()
														.Contains(
																"namespace SQE.API.Server.HttpControllers"));

			// Collect all the methods in the services namespace
			var serviceMethods = await Parsers.GetAllSyntaxNodesAsync<MethodDeclarationSyntax>(
					compilation
					, semModels
					, new List<string> { "SQE.API.Server.Services" });

			// Collect all the methods in the services namespace
			var broadcastMethods =
					(await Parsers.GetAllSyntaxNodesAsync<InterfaceDeclarationSyntax>(
							compilation
							, semModels
							, new List<string> { "SQE.API.Server.RealtimeHubs" }))
					.SelectMany(x => x.Syntax.Members)
					.OfType<MethodDeclarationSyntax>();

			var completeListenerList = new List<ParameterDescription>();

			// Begin walking the syntax tree in order to parse the controller classes
			foreach (var tree in compilation.SyntaxTrees)
			{
				var rootSyntaxNode = await tree.GetRootAsync();

				// Look at each class declaration
				foreach (var node in rootSyntaxNode.DescendantNodes()
												   .OfType<ClassDeclarationSyntax>())
				{
					// Ignore all classes except those in the HttpControllers namespace
					NamespaceDeclarationSyntax namespaceDeclarationSyntax = null;

					if (!Helpers.SyntaxNodeHelper.TryGetParentSyntax(
							node
							, out namespaceDeclarationSyntax))
						continue;

					if (namespaceDeclarationSyntax.Name.ToString()
						!= "SQE.API.Server.HttpControllers")
						continue;

					completeListenerList = completeListenerList.Concat(
																	   await Parsers
																			   .ParseAndWriteClassesAsync(
																					   testFolder
																					   , node
																					   , semModels
																					   , serviceMethods
																					   , broadcastMethods
																					   , project))
															   .ToList();
				}
			}

			// Write the Enum with completeListenerList
			var listenerEnumPath = Path.Combine(testFolder, "ApiRequests", "ListenerMethods.cs");

			Console.WriteLine($"Writing listener methods enum to {listenerEnumPath}");

			completeListenerList = completeListenerList.Distinct().ToList();

			using (var outputFile = new StreamWriter(listenerEnumPath))
				await Writers.WriteListenerEnumsAsync(completeListenerList, outputFile);

			// Finish
			Console.WriteLine("Successfully parsed all endpoints.");
		}
	}
}
