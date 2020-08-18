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
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Buildalyzer;
using CaseExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Buildalyzer.Workspaces;

namespace GenerateTestRequestObjects
{
    internal static class Program
    {
        

        private static readonly Regex _rx = new Regex(
            @"Task?<(?<return>.*)?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static async Task Main(string[] args)
        {
            //MSBuildLocator.RegisterDefaults();
            await ParseSqeHttpControllers();
        }

        private static async Task ParseSqeHttpControllers()
        {
            Console.WriteLine("Parsing the HTTP controllers and creating corresponding ApiRequest Objects.");

            // TODO: Can we find a better way to resolve these paths instead of all the backtracking?
            var projectRoot =
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../", "../", "../", "../"));
            var ApiServerRoot = Path.GetFullPath(Path.Combine(projectRoot, "../", "sqe-api-server"));
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
                .Where(x => 
                    x.SyntaxTree.GetRoot().ToString().Contains("namespace SQE.API.Server.Services")
                    || x.SyntaxTree.GetRoot().ToString().Contains("namespace SQE.API.Server.HttpControllers"));
            
            // Collect all the syntax trees for the services namespace
            // Hold the MethodDeclarationSyntax and the symbol name for later usage
            var allMethods = (await
                compilation.SyntaxTrees.SelectManyAsync(async x =>
                    {
                        var results = (await x.GetRootAsync()).DescendantNodes().OfType<MethodDeclarationSyntax>()
                            .Where(x =>
                            {
                                NamespaceDeclarationSyntax namespaceDeclarationSyntax = null;
                                if (!SyntaxNodeHelper.TryGetParentSyntax(x, out namespaceDeclarationSyntax)) 
                                    return false;
                                return namespaceDeclarationSyntax.Name.ToString() == "SQE.API.Server.Services" 
                                       && x.Body != null;
                            });
                        return results;
                    }
                    )
                )
                .Select(x =>
                {
                    foreach (var semModel in semModels)
                    {
                        try
                        {
                            var invokedSymbol = semModel.GetDeclaredSymbol(x);
                            if (invokedSymbol == null) 
                                continue;
                            return new
                            {
                                Syntax = x, 
                                Symbol = invokedSymbol.ToString()
                            };
                        }
                        catch
                        {
                        }
                    }
                    return new {Syntax = x, Symbol = default(string)};
                });

            // Begin walking the syntax tree in order to parse the controller classes
            foreach (var tree in compilation.SyntaxTrees)
            {
                var rootSyntaxNode = await tree.GetRootAsync();

                // Look at each class declaration
                foreach (var node in rootSyntaxNode.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    // Ignore all classes except those in the HttpControllers namespace
                    NamespaceDeclarationSyntax namespaceDeclarationSyntax = null;
                    if (!SyntaxNodeHelper.TryGetParentSyntax(node, out namespaceDeclarationSyntax)) continue;
                    if (namespaceDeclarationSyntax.Name.ToString() != "SQE.API.Server.HttpControllers") continue;

                    var controllerName = node.Identifier.ValueText.ToLowerInvariant().Replace("controller", "");
                    Console.WriteLine($"Writing to {Path.Combine(testFolder, "ApiRequests", $"test-{controllerName}Requests.cs")}");
                    using (var outputFile = new StreamWriter(Path.Combine(testFolder, "ApiRequests",
                        $"test-{controllerName}Requests.cs")))
                    {
                        // Begin writing file
                        outputFile.WriteLine(@"
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
");

                        var parsedControllerMethods = new ApiRequestsDescription();
                        
                        // Look at each method in the controller class
                        foreach (var method in node.DescendantNodes().OfType<MethodDeclarationSyntax>())
                        {
                            var statements = method.Body.Statements;
                            var function = statements.FirstOrDefault().DescendantNodes();
                            var methodDescription = new ApiRequestEndpointDescription
                            {
                                comments = method.GetLeadingTrivia().ToString().Trim()
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
                            methodDescription.OType = oType.ToString() == "ActionResult" ? "EmptyOutput" : oType.ToString();
                            
                            // Format the method name and grab authorization
                            var (anonymousAllowed, methodName, httpRequestType, httpPath) =
                                GetMethodNameAndAuthorization(method, controllerName);

                            methodDescription.HttpString = httpPath.Replace("[controller]", controllerName);

                            // Format the method parameters
                            foreach (var param in method.ParameterList.Parameters)
                            {
                                if (param.AttributeLists.ToString().Contains("FromBody") &&
                                    methodDescription.bodyParams == null)
                                {
                                    methodDescription.bodyParams =
                                        new ParameterDescription(param.Type.ToString(), "payload");
                                    methodDescription.IType = param.Type.ToString();
                                }
                                    
                                    
                                if (param.AttributeLists.ToString().Contains("FromRoute"))
                                    methodDescription.routeParams
                                        .Add(new ParameterDescription(param.Type.ToString(), param.Identifier.ToString()));
                                
                                if (param.AttributeLists.ToString().Contains("FromQuery"))
                                    methodDescription.queryParams
                                        .Add(new ParameterDescription(param.Type.ToString(), "optional = null"));
                            }
                            
                            switch (httpRequestType.ToLowerInvariant())
                            {
                                case "get":
                                    if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Get))
                                        parsedControllerMethods.requests.TryAdd(HttpMethod.Get,
                                            new List<ApiRequestEndpointDescription>());
                                    if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Get, out var lsGet))
                                        lsGet.Add(methodDescription);
                                    break;
                                case "post":
                                    if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Post))
                                        parsedControllerMethods.requests.TryAdd(HttpMethod.Post,
                                            new List<ApiRequestEndpointDescription>());
                                    if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Post, out var lsPost))
                                        lsPost.Add(methodDescription);
                                    break;
                                case "put":
                                    if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Put))
                                        parsedControllerMethods.requests.TryAdd(HttpMethod.Put,
                                            new List<ApiRequestEndpointDescription>());
                                    if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Put, out var lsPut))
                                        lsPut.Add(methodDescription);
                                    break;
                                case "delete":
                                    if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Delete))
                                        parsedControllerMethods.requests.TryAdd(HttpMethod.Delete,
                                            new List<ApiRequestEndpointDescription>());
                                    if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Delete, out var lsDelete))
                                        lsDelete.Add(methodDescription);
                                    break;
                                default:
                                    throw new Exception("Unsupported http verb");
                            }
                    
                            // Get the first invoked expression
                            // The rigid design practice dictates that it will be the call to the service
                            var invokedMethod = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
                                .FirstOrDefault();
                            var foundInvokedMethod = false;

                            // Start searching the semantic model for this expression
                            foreach (var semModel in semModels)
                            {
                                try
                                {
                                    // Get the symbol, which will refer to the interface
                                    var invokedSymbol = semModel.GetSymbolInfo(invokedMethod).Symbol;
                                    if (invokedSymbol == null) continue;

                                    // Get the actual implementation of the interface
                                    var implementations =
                                        await SymbolFinder.FindImplementationsAsync(invokedSymbol, project.Solution);
                                    invokedSymbol = implementations.First();
                                    foundInvokedMethod = true;

                                    // Get the MethodDeclarationSyntax for the called expression
                                    // (so we can inspect its body for SignalR broadcasts
                                    var invokedSyntax = allMethods.FirstOrDefault(x =>
                                        x.Symbol == invokedSymbol.ToString());
                                    // Console.WriteLine($" *** {method.Kind()}");
                                    // Console.WriteLine($"     {method}");
                                    // Console.WriteLine("Invokes:");
                                    // Console.WriteLine(invokedSyntax.Syntax.Body);
                                    
                                    // TODO: Parse the listenermethod and put it into the RequestObject so it is written to the file
                                }
                                catch
                                {
                                }
                            }

                            if (!foundInvokedMethod)
                                throw new Exception(
                                    $@"Could not find a method declaration for {invokedMethod.ToString()},
called in method {method.ToString()} of class {node.ToString()}");
                            
                        }
                        
                    foreach (var httpVerb in parsedControllerMethods.requests.Keys)
                        {
                            outputFile.WriteLine(@$"
            public static partial class {httpVerb}
            {{
        ");
                            if (!parsedControllerMethods.requests.TryGetValue(httpVerb, out var endpoints)) continue;
                            foreach (var endpoint in endpoints)
                            {
                                var endpointInputType = endpoint.IType == null ? "EmptyInput" : endpoint.IType.ToString();
                                var endpointOutputType = endpoint.OType == null ? "EmptyOutput" : endpoint.OType.ToString();
                                var endpointListenerType = httpVerb.ToString().ToLowerInvariant() switch
                                {
                                    "get" => "EmptyOutput",
                                    "post" => endpointOutputType,
                                    "put" => endpointOutputType,
                                    "delete" => "DeleteDTO",
                                    _ => throw new Exception("Unsupported http verb")
                                };
                                var methodName = endpoint.HttpString.Replace("\"", "")
                                    .ToPascalCase()
                                    .Replace("/", "_")
                                    .Replace("{", "")
                                    .Replace("}", "");
                                var constructorParams = string.Join(",", new List<ParameterDescription>()
                                    .Concat(endpoint.routeParams)
                                    .Concat(new List<ParameterDescription>() {endpoint.bodyParams})
                                    .Concat(endpoint.queryParams)
                                    .Where(x => x != null)
                                    .Select(x => $"{x.ParamType.ToString()} {x.ParamName}"));
                                var baseConstructorParams = new List<string>();
                                if (endpoint.routeParams.Any(x => x.ParamName == "editionId"))
                                    baseConstructorParams.Add("editionId");
                                if (endpoint.queryParams.Any())
                                    baseConstructorParams.Add("optional");
                                if (endpoint.routeParams.Any(x => x.ParamName == "editionId") && !endpoint.queryParams.Any())
                                    baseConstructorParams.Add("null");
                                if (endpoint.bodyParams != null)
                                    baseConstructorParams.Add("payload");
                                outputFile.WriteLine($@"
                public class {methodName}
                : {(endpoint.routeParams.Any(x => x.ParamName == "editionId") ? "EditionRequestObject" : "RequestObject")}<{endpointInputType}, {endpointOutputType}, {endpointListenerType}>
                {{
                    {endpoint.comments}
                    public {methodName}({constructorParams}) 
                        : base({string.Join(", ", baseConstructorParams)}) {{ }}
                }}
        ");
                                
                            }
                            outputFile.WriteLine("\t}");
                        }
                        // Write ending to the file
                        outputFile.WriteLine("\n}");
                    }
                }
            }
            
            Console.WriteLine($"Successfully parsed all endpoints.");

            //Console.ReadKey();

            // // Parse the code to the relevant members
            // var code = new StreamReader(csProjFile.FullName).ReadToEnd();
            // var tree = CSharpSyntaxTree.ParseText(code);
            // var root = tree.GetCompilationUnitRoot();
            // var members = root.DescendantNodes().OfType<MemberDeclarationSyntax>();
            //
            // // Format and write the Hub controller
            // WriteHubController(classFields, projectRoot, hubFolder);
            //
            // // Format and write the Hub interface——We do not do this now
            // //WriteHubInterface(hubInterfaces, projectRoot, hubFolder);
            //
            // // Format and write the custom subscription controller
            // CopySubscriptionHubController(projectRoot, hubFolder);
        }

    //     private static (List<ControllerField>, List<string>) ParseSqeHttpControllers(string hubFolder, FileInfo file)
    //     {
    //         /*
			 // * Note: I believe we now catch all instances of attribute routing that cannot
			 // * be resolved into a SignalR hub method name.
			 // */
    //
    //         var controllerName = file.Name.Replace("Controller.cs", "");
    //         Console.WriteLine($"Parsing {file.FullName}");
    //
    //         // Parse the code to the relevant members
    //         var code = new StreamReader(file.FullName).ReadToEnd();
    //         var tree = CSharpSyntaxTree.ParseText(code);
    //         var root = tree.GetCompilationUnitRoot();
    //         var members = root.DescendantNodes().OfType<MemberDeclarationSyntax>();
    //
    //         // Verify the controller is properly formatted with no disallowed attributes
    //         VerifyControllerClassName(members);
    //
    //         var hubInterfaces = WriteHub(hubFolder, controllerName, root, members);
    //         WriteTestingApiRequest(Path.Combine(hubFolder, "..", "..", "sqe-api-test"), controllerName, root, members);
    //
    //         // return info for any dependency injected fields
    //         return (AnalyzeController(members), hubInterfaces);
    //     }

        private static void VerifyControllerClassName(IEnumerable<MemberDeclarationSyntax> members)
        {
            // Get classes and ensure there is only one in the file
            var classes = members.OfType<ClassDeclarationSyntax>().ToList();
            if (classes.Count != 1)
                throw new Exception(
                    $"Each Controller file should have only one class, this file has {classes.Count} classes."
                );

            // Grab the controller class and ensure it only has two attributes
            var controllerClass = classes.First();
            var controllerAttrs = controllerClass.AttributeLists.ToList();
            if (controllerAttrs.Count > 2)
                throw new Exception(
                    "The SQE API only supports controller classes with the [Authorize] and [ApiController] attributes."
                );

            var isControllerClass = false;
            var isAuthorized = false;
            var hasPath = false;

            // Loop over attributes to ensure it has one ApiController attribute, one Authorize attribute, and no
            // route paths.
            foreach (var attr in controllerAttrs)
            {
                if (attr.Attributes.Count(x => x.Name.ToString() == "ApiController") == 1)
                    isControllerClass = true;
                if (attr.Attributes.Count(x => x.Name.ToString() == "Authorize") == 1)
                    isAuthorized = true;
                if (attr.Attributes.Count(
                        x =>
                            x.Name.ToString().IndexOf("Route", StringComparison.CurrentCultureIgnoreCase) >= 0
                            || x.Name.ToString().IndexOf("Http", StringComparison.CurrentCultureIgnoreCase) >= 0
                    )
                    > 0)
                    hasPath = true;
            }

            if (!isControllerClass)
                throw new Exception("This controller class must have the attribute [ApiController].");
            if (!isAuthorized)
                throw new Exception("This controller class must have the attribute [Authorize].");
            if (hasPath)
                throw new Exception("The SQE API does not support controllers with class level routing attributes.");
        }

        // private static List<string> WriteHub(string hubFolder, string controllerName, CompilationUnitSyntax root, IEnumerable<MemberDeclarationSyntax> members)
        // {
        //     var hubInterfaces = new List<string>();
        //     
        //     // Begin writing analyzed controller to Hub
        //     Console.WriteLine($"Writing to {Path.Combine(hubFolder, $"{controllerName}Hub.cs")}");
        //     using (var outputFile = new StreamWriter(Path.Combine(hubFolder, $"{controllerName}Hub.cs")))
        //     {
        //         // Write disclaimer to file
        //         outputFile.Write(_autogenFileDisclaimer);
        //
        //         // Collect and write using statements
        //         foreach (var element in root.Usings.Where(
        //             element => element.Name.ToString() != "Microsoft.AspNetCore.Mvc"
        //         ))
        //             outputFile.WriteLine($"using {element.Name};");
        //         outputFile.WriteLine("using Microsoft.AspNetCore.SignalR;\n");
        //         outputFile.WriteLine("using SQE.DatabaseAccess.Helpers;\n");
        //         outputFile.WriteLine("using System.Text.Json;\n");
        //         outputFile.WriteLine("using SQE.API.Server.Helpers;\n");
        //
        //         // Write hub class declaration
        //         outputFile.WriteLine(_hubClassTemplate);
        //
        //         // Parse individual methods
        //         foreach (var method in members.OfType<MethodDeclarationSyntax>().ToList())
        //         {
        //             // Copy the comments
        //             WriteMethodCommentsToFile(method, outputFile);
        //
        //             // Format the method modifiers
        //             var methodModifiers = string.Join(" ", method.Modifiers.Select(x => x.Text));
        //
        //             // Format the return type
        //             var returnType = ProcessReturnType(method);
        //
        //             // Format the method name and grab authorization
        //             var (anonymousAllowed, methodName, httpRequestType, httpPath) =
        //                 GetMethodNameAndAuthorization(method, controllerName);
        //
        //             // Find mutate requests for Hub interface
        //             var hubInterface = ParseHubInterface(
        //                 httpRequestType,
        //                 httpPath.Replace("[controller]", controllerName),
        //                 returnType
        //             );
        //             if (!string.IsNullOrEmpty(hubInterface))
        //                 hubInterfaces.Add(hubInterface);
        //
        //             // Format the method parameters
        //             var methodParams = string.Join(
        //                 ", ",
        //                 method.ParameterList.Parameters.Select(x => $"{x.Type} {x.Identifier}")
        //             );
        //
        //             // Format the method signature from method modifiers, return type, name, and parameters
        //             var methodSignature = $"{methodModifiers} {returnType} {methodName}({methodParams})";
        //
        //             // Write the method signature
        //             outputFile.WriteLine(anonymousAllowed ? "[AllowAnonymous]" : "[Authorize]");
        //             outputFile.WriteLine(methodSignature);
        //
        //             // Format the method body and add clientId to mutate requests
        //             var methodBody = Regex.Replace(
        //                 method.Body.ToString()
        //                     .Replace("\n", "")
        //                     .Replace("\t", "")
        //                     .Replace("{ ", "")
        //                     .Replace(" }", "")
        //                     .Replace("return ", returnType == "Task" ? "" : "return "),
        //                 @"\s*\)\s*;",
        //                 httpRequestType == "Get" ? ");" : ", clientId: Context.ConnectionId);"
        //             );
        //
        //             // Write the method body
        //             outputFile.WriteLine(_hubMethod.Replace("$Method", methodBody));
        //             outputFile.WriteLine("");
        //         }
        //
        //         // Write ending to the file
        //         outputFile.WriteLine("\t}\n}");
        //     }
        //
        //     return hubInterfaces;
        // }
        
        private static void WriteTestingApiRequest(string testFolder, string controllerName, CompilationUnitSyntax root, IEnumerable<MemberDeclarationSyntax> members)
        {
            // Begin writing analyzed controller to Hub
            Console.WriteLine($"Writing to {Path.Combine(testFolder, "ApiRequests", $"test-{controllerName}Requests.cs")}");
            using (var outputFile = new StreamWriter(Path.Combine(testFolder, "ApiRequests", $"test-{controllerName}Requests.cs")))
            {
                // Write disclaimer to file
                //outputFile.Write(_autogenFileDisclaimer);

                // Write using statements and namespace
                outputFile.WriteLine(@"
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
");

                var parsedControllerMethods = new ApiRequestsDescription();
                // Parse individual methods
                foreach (var method in members.OfType<MethodDeclarationSyntax>().ToList())
                {
                    var statements = method.Body.Statements;
                    var function = statements.FirstOrDefault().DescendantNodes();
                    var methodDescription = new ApiRequestEndpointDescription
                    {
                        comments = method.GetLeadingTrivia().ToString().Trim()
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
                    methodDescription.OType = oType.ToString() == "ActionResult" ? "EmptyOutput" : oType.ToString();
                    
                    // Format the method name and grab authorization
                    var (anonymousAllowed, methodName, httpRequestType, httpPath) =
                        GetMethodNameAndAuthorization(method, controllerName);

                    methodDescription.HttpString = httpPath.Replace("[controller]", controllerName);

                    // Format the method parameters
                    foreach (var param in method.ParameterList.Parameters)
                    {
                        if (param.AttributeLists.ToString().Contains("FromBody") &&
                            methodDescription.bodyParams == null)
                        {
                            methodDescription.bodyParams =
                                new ParameterDescription(param.Type.ToString(), "payload");
                            methodDescription.IType = param.Type.ToString();
                        }
                            
                            
                        if (param.AttributeLists.ToString().Contains("FromRoute"))
                            methodDescription.routeParams
                                .Add(new ParameterDescription(param.Type.ToString(), param.Identifier.ToString()));
                        
                        if (param.AttributeLists.ToString().Contains("FromQuery"))
                            methodDescription.queryParams
                                .Add(new ParameterDescription(param.Type.ToString(), "optional = null"));
                    }
                    
                    switch (httpRequestType.ToLowerInvariant())
                    {
                        case "get":
                            if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Get))
                                parsedControllerMethods.requests.TryAdd(HttpMethod.Get,
                                    new List<ApiRequestEndpointDescription>());
                            if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Get, out var lsGet))
                                lsGet.Add(methodDescription);
                            break;
                        case "post":
                            if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Post))
                                parsedControllerMethods.requests.TryAdd(HttpMethod.Post,
                                    new List<ApiRequestEndpointDescription>());
                            if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Post, out var lsPost))
                                lsPost.Add(methodDescription);
                            break;
                        case "put":
                            if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Put))
                                parsedControllerMethods.requests.TryAdd(HttpMethod.Put,
                                    new List<ApiRequestEndpointDescription>());
                            if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Put, out var lsPut))
                                lsPut.Add(methodDescription);
                            break;
                        case "delete":
                            if (!parsedControllerMethods.requests.ContainsKey(HttpMethod.Delete))
                                parsedControllerMethods.requests.TryAdd(HttpMethod.Delete,
                                    new List<ApiRequestEndpointDescription>());
                            if (parsedControllerMethods.requests.TryGetValue(HttpMethod.Delete, out var lsDelete))
                                lsDelete.Add(methodDescription);
                            break;
                        default:
                            throw new Exception("Unsupported http verb");
                    }
                }

                foreach (var httpVerb in parsedControllerMethods.requests.Keys)
                {
                    outputFile.WriteLine(@$"
    public static partial class {httpVerb}
    {{
");
                    if (!parsedControllerMethods.requests.TryGetValue(httpVerb, out var endpoints)) continue;
                    foreach (var endpoint in endpoints)
                    {
                        var endpointInputType = endpoint.IType == null ? "EmptyInput" : endpoint.IType.ToString();
                        var endpointOutputType = endpoint.OType == null ? "EmptyOutput" : endpoint.OType.ToString();
                        var endpointListenerType = httpVerb.ToString().ToLowerInvariant() switch
                        {
                            "get" => "EmptyOutput",
                            "post" => endpointOutputType,
                            "put" => endpointOutputType,
                            "delete" => "DeleteDTO",
                            _ => throw new Exception("Unsupported http verb")
                        };
                        var methodName = endpoint.HttpString.Replace("\"", "")
                            .ToPascalCase()
                            .Replace("/", "_")
                            .Replace("{", "")
                            .Replace("}", "");
                        var constructorParams = string.Join(",", new List<ParameterDescription>()
                            .Concat(endpoint.routeParams)
                            .Concat(new List<ParameterDescription>() {endpoint.bodyParams})
                            .Concat(endpoint.queryParams)
                            .Where(x => x != null)
                            .Select(x => $"{x.ParamType.ToString()} {x.ParamName}"));
                        var baseConstructorParams = new List<string>();
                        if (endpoint.routeParams.Any(x => x.ParamName == "editionId"))
                            baseConstructorParams.Add("editionId");
                        if (endpoint.queryParams.Any())
                            baseConstructorParams.Add("optional");
                        if (endpoint.routeParams.Any(x => x.ParamName == "editionId") && !endpoint.queryParams.Any())
                            baseConstructorParams.Add("null");
                        if (endpoint.bodyParams != null)
                            baseConstructorParams.Add("payload");
                        outputFile.WriteLine($@"
        public class {methodName}
        : {(endpoint.routeParams.Any(x => x.ParamName == "editionId") ? "EditionRequestObject" : "RequestObject")}<{endpointInputType}, {endpointOutputType}, {endpointListenerType}>
        {{
            {endpoint.comments}
            public {methodName}({constructorParams}) 
                : base({string.Join(", ", baseConstructorParams)}) {{ }}
        }}
");
                        
                    }
                    outputFile.WriteLine("\t}");
                }
                
                // Write ending to the file
                outputFile.WriteLine("\n}");
            }
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
                ? Regex.Replace(
                    method.ReturnType.ToString().Replace(actionResultString, ""),
                    ">$",
                    ""
                )
                : method.ReturnType.ToString();
        }

        private static (bool, string, string, string) GetMethodNameAndAuthorization(MethodDeclarationSyntax method,
            string controllerName)
        {
            var validPathAttrs = new List<string> { "HttpGet", "HttpPost", "HttpPut", "HttpDelete" };
            var httpRequestType = "";
            var httpPath = "";
            var formattedPath = "";
            var anonymousAllowed = false;
            var attributes = method.AttributeLists.SelectMany(attributeList => attributeList.Attributes);

            // Reject any controller endpoints with complex routing
            if (attributes.Count(
                    x =>
                        x.Name.ToString().IndexOf("Route", StringComparison.CurrentCultureIgnoreCase) >= 0
                        || x.Name.ToString().IndexOf("Http", StringComparison.CurrentCultureIgnoreCase) >= 0
                )
                > 1)
                throw new Exception(
                    $"The SQE API only supports endpoints with a single route path attribute. The method {method.Identifier.Text} violates this policy."
                );
            foreach (var attribute in attributes)
                if (attribute.Name.ToString() == "AllowAnonymous")
                {
                    anonymousAllowed = true;
                }
                else
                {
                    var attributeName = attribute.Name.ToString();
                    if (!validPathAttrs.Contains(attributeName, StringComparer.CurrentCultureIgnoreCase))
                        throw new Exception($"The SQE API does not support the attribute {attributeName}.");

                    httpRequestType = attributeName.Replace("Http", "");
                    foreach (var argument in attribute.ArgumentList.Arguments)
                    {
                        httpPath = argument.ToString();
                        formattedPath = string.Join(
                            "",
                            httpPath
                                .Replace("[controller]", controllerName)
                                .Replace("[action]", method.Identifier.Text)
                                .Replace("{", "")
                                .Replace("}", "")
                                .Replace("\"", "")
                                .Split("/")
                                .Select(x => x.ToPascalCase())
                        );

                        // Reject any routes using token replacement with tokens other than [controller] or [action].
                        if (formattedPath.Contains("[")
                            || formattedPath.Contains("]"))
                            throw new Exception(
                                $"The SQE API only allows paths using token replacement with the [controller] and [action] tokens. The method {method.Identifier.Text} violates this policy."
                            );
                    }
                }

            return (anonymousAllowed, $"{httpRequestType}{formattedPath}", httpRequestType, httpPath);
        }

        private static List<ControllerField> AnalyzeController(IEnumerable<MemberDeclarationSyntax> elements)
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

        private static void ValidateControllerConstructor(ConstructorDeclarationSyntax cst,
            List<ControllerField> fields)
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
                    ? paramMatches.Groups[1].ToString()
                    : "";

                paramMatchNotFound = tempFieldList.Count(x => x.type == type && x.name == assignedTo) != 1;
                tempFieldList = tempFieldList.Where(x => x.type != type && x.name != assignedTo).ToList();
            }

            if (paramMatchNotFound || tempFieldList.Any())
                throw new Exception(
                    "Error: The constructor for this controller does not set a value to every instance variable."
                );
        }

        // TODO: I am not really happy with automatically creating the interface for the remote methods called by
        // SignalR hub. Probably these should be manually created.
        private static string ParseHubInterface(string responseHttpType, string httpPath, string responseType)
        {
            string type = null;
            string item = null;

            // Get the response object type 
            var matches = _rx.Matches(responseType);
            if (matches.Count > 1)
                return null;
            responseType = matches.Count == 1 ? matches[0].Groups["return"].Value : null;

            // Format the method HTTP type and the method item name
            switch (responseHttpType)
            {
                // For a post (item =, e.g., .../rois/batch -> rois-batch; .../rois -> roi)
                case "Post":
                    type = "Create";
                    item = httpPath.Contains("/batch")
                        ? string.Join("-", httpPath.Split("/").Reverse().Take(2).Reverse())
                        : httpPath.Split("/").LastOrDefault()?.TrimEnd('"').TrimEnd('s');
                    break;
                // For a put (item =, e.g., .../rois/batch -> rois-batch; .../rois/{roiId} -> roi)
                case "Put":
                    type = "Update";
                    item = httpPath.Contains("/batch")
                        ? string.Join("-", httpPath.Split("/").Reverse().Take(2).Reverse())
                        : httpPath.Split("{").LastOrDefault()?.Split("Id}").FirstOrDefault();
                    break;
                // For a delete (item =, e.g., .../rois/{roiId} -> roi)
                case "Delete":
                    type = "Delete";
                    responseType =
                        responseType ?? "uint"; // Assume a delete just returns the uint id of the deleted item
                    item = httpPath.Split("{").LastOrDefault()?.Split("Id}").FirstOrDefault();
                    break;
            }

            // Give up if we don't have a type or responseType
            if (string.IsNullOrEmpty(type)
                || string.IsNullOrEmpty(responseType))
                return null;

            // Change item to PascalCase for use as a method name
            item = item.ToPascalCase().Replace("{", "").Replace("}", "").Replace("\"", "");

            // Return null if item is null or some improperly formatted string
            return string.IsNullOrEmpty(item) || item.Contains("/")
                ? null
                : $"\t\tTask {type}{char.ToUpper(item[0]) + item.Substring(1)}({responseType} returnedData);";
        }

        // private static void WriteHubController(IReadOnlyCollection<ControllerField> fields, string projectRoot, string hubFolder)
        // {
        //     var template = new StreamReader($"{projectRoot}/HubConstructorTemplate.txt").ReadToEnd();
        //     template = template
        //         .Replace(
        //             "$Fields",
        //             string.Join("\n", fields.Select(x => $"\t\t{x.modifiers} {x.type} {x.name};"))
        //         )
        //         .Replace("$Params", string.Join(", ", fields.Select(x => $"{x.type} {x.name.Replace("_", "")}")))
        //         .Replace(
        //             "$Body",
        //             string.Join("\n", fields.Select(x => $"\t\t\t{x.name} = {x.name.Replace("_", "")};"))
        //         );
        //     using (var outputFile = new StreamWriter($"{hubFolder}/HubConstructor.cs"))
        //     {
        //         outputFile.Write(_autogenFileDisclaimer);
        //         outputFile.Write(template);
        //     }
        // }

        private static void WriteHubInterface(List<string> methods, string projectRoot, string hubFolder)
        {
            var template = new StreamReader($"{projectRoot}/HubInterfaceTemplate.txt").ReadToEnd();
            template = template
                .Replace(
                    "$Methods",
                    string.Join("\n", methods)
                );
            using (var outputFile = new StreamWriter($"{hubFolder}/HubInterface.cs"))
            {
                outputFile.Write(template);
            }
        }

        // private static void CopySubscriptionHubController(string projectRoot, string hubFolder)
        // {
        //     var subscriptionHub = new StreamReader(Path.Combine(projectRoot, "SubscriptionHub.cs.txt")).ReadToEnd();
        //     using (var outputFile = new StreamWriter(Path.Combine(hubFolder, "SubscriptionHub.cs")))
        //     {
        //         outputFile.Write(_subscriptionHubDisclaimer);
        //         outputFile.Write(subscriptionHub);
        //     }
        // }

        private class ControllerField
        {
            public ControllerField(string modifiers, string name, string type)
            {
                this.modifiers = modifiers;
                this.name = name;
                this.type = type;
            }

            public string modifiers { get; }
            public string name { get; }
            public string type { get; }
        }

        private class FieldComparer : IEqualityComparer<ControllerField>
        {
            public bool Equals(ControllerField x, ControllerField y)
            {
                return x != null
                       && y != null
                       && x.modifiers.Equals(y.modifiers)
                       && x.name.Equals(y.name)
                       && x.type.Equals(y.type);
            }

            public int GetHashCode(ControllerField obj)
            {
                var md5Hasher = MD5.Create();
                var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(obj.modifiers + obj.name + obj.type));
                return BitConverter.ToInt32(hashed, 0);
            }
        }

        private class ParameterDescription
        {
            public ParameterDescription(string paramType, string paramName)
            {
                ParamType = paramType;
                ParamName = paramName;
            }

            public string ParamType { get; set; }
            public string ParamName { get; set; }
        }

        private class ApiRequestEndpointDescription
        {
            public ApiRequestEndpointDescription()
            {
                this.routeParams = new List<ParameterDescription>();
                this.queryParams = new List<ParameterDescription>();
            }

            public string IType { get; set; }
            public string OType { get; set; }
            public string ListenerType { get; set; }
            public string HttpString { get; set; }
            public List<ParameterDescription> routeParams { get; set; }
            public List<ParameterDescription> queryParams { get; set; }
            public ParameterDescription bodyParams { get; set; }
            public string comments { get; set; }
        }

        private class ApiRequestsDescription
        {
            public ApiRequestsDescription()
            {
                this.requests = new Dictionary<HttpMethod, List<ApiRequestEndpointDescription>>();
            }
            public Dictionary<HttpMethod, List<ApiRequestEndpointDescription>> requests{ get; set; }
        }
        
        /// <summary>
        ///     An empty request payload object
        /// </summary>
        public class EmptyInput
        {
        }

        /// <summary>
        ///     An empty request response object
        /// </summary>
        public class EmptyOutput
        {
        }
        
        static class SyntaxNodeHelper
        {
            public static bool TryGetParentSyntax<T>(SyntaxNode syntaxNode, out T result) 
                where T : SyntaxNode
            {
                // set defaults
                result = null;

                if (syntaxNode == null)
                {
                    return false;
                }

                try
                {
                    syntaxNode = syntaxNode.Parent;

                    if (syntaxNode == null)
                    {
                        return false;
                    }

                    if (syntaxNode.GetType() == typeof (T))
                    {
                        result = syntaxNode as T;
                        return true;
                    }

                    return TryGetParentSyntax<T>(syntaxNode, out result);
                }
                catch
                {
                    return false;
                }
            }
        }
        
        public static async Task<IEnumerable<TResult>> SelectAsync<TSource,TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> method)
        {
            return await Task.WhenAll(source.Select(async s => await method(s)));
        }
        
        // public static async Task<IEnumerable<TResult>> SelectManyAsync<TSource,TResult>(this IEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> method)
        // {
        //     return await Task.WhenAll(source.SelectMany<TSource,TResult>(s => method(s)));
        // }
        
        public static async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(
            this IEnumerable<T> enumeration,
            Func<T, Task<IEnumerable<T1>>> func)
        {
            return (await enumeration.SelectAsync(func)).SelectMany(x => x);
        }
    }
}