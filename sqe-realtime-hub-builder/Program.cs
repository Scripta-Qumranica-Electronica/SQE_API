﻿/*
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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CaseExtensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace sqe_realtime_hub_builder
{
    internal static class Program
    {
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

        private static void Main(string[] args)
        {
            ParseSqeHttpControllers();
        }

        private static void ParseSqeHttpControllers()
        {
            Console.WriteLine("Parsing the HTTP controllers and creating corresponding SignalR hubs.");

            // TODO: Can we find a better way to resolve these paths instead of all the backtracking?
            var projectRoot =
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../", "../", "../"));
            var ApiServerRoot = Path.GetFullPath(Path.Combine(projectRoot, "../", "sqe-api-server"));
            var controllerFolder = Path.GetFullPath(Path.Combine(ApiServerRoot, "HttpControllers"));
            var hubFolder = Path.GetFullPath(Path.Combine(ApiServerRoot, "RealtimeHubs"));

            var dir = new DirectoryInfo(controllerFolder);
            var files = dir.GetFiles("*.cs");
            var classFields = new List<ControllerField>();

            // Delete any existing Realtime Hubs
            Console.WriteLine($"Deleting any existing realtime hub methods from {hubFolder}.");
            var folderInfo = new DirectoryInfo(hubFolder);
            foreach (var file in folderInfo.GetFiles()) file.Delete();

            // Parse each controller file
            foreach (var file in files)
                classFields = classFields.Union(ParseSqeHttpControllers(hubFolder, file), new FieldComparer()).ToList();

            // Format and write the Hub controller
            WriteHubController(classFields, projectRoot, hubFolder);

            // Format and write the custom subscription controller
            CopySubscriptionHubController(projectRoot, hubFolder);
        }

        private static List<ControllerField> ParseSqeHttpControllers(string hubFolder, FileInfo file)
        {
            /*
			 * Note: I believe we now catch all instances of attribute routing that cannot
			 * be resolved into a SignalR hub method name.
			 */

            var controllerName = file.Name.Replace("Controller.cs", "");
            Console.WriteLine($"Parsing {file.FullName}");

            // Parse the code to the relevant members
            var code = new StreamReader(file.FullName).ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var members = root.DescendantNodes().OfType<MemberDeclarationSyntax>().ToList();

            // Verify the controller is properly formatted with no disallowed attributes
            VerifyControllerClassName(members);

            // Begin writing analyzed controller to Hub
            Console.WriteLine($"Writing to {Path.Combine(hubFolder, $"{controllerName}Hub.cs")}");
            using (var outputFile = new StreamWriter(Path.Combine(hubFolder, $"{controllerName}Hub.cs")))
            {
                // Write disclaimer to file
                outputFile.Write(_autogenFileDisclaimer);

                // Collect and write using statements
                foreach (var element in root.Usings.Where(
                    element => element.Name.ToString() != "Microsoft.AspNetCore.Mvc"
                ))
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
                    var (anonymousAllowed, methodName, httpRequestType) =
                        GetMethodNameAndAuthorization(method, controllerName);

                    // Format the method parameters
                    var methodParams = string.Join(
                        ", ",
                        method.ParameterList.Parameters.Select(x => $"{x.Type} {x.Identifier}")
                    );

                    // Format the method signature from method modifiers, return type, name, and parameters
                    var methodSignature = $"{methodModifiers} {returnType} {methodName}({methodParams})";

                    // Write the method signature
                    outputFile.WriteLine(anonymousAllowed ? "[AllowAnonymous]" : "[Authorize]");
                    outputFile.WriteLine(methodSignature);

                    // Format the method body and add clientId to mutate requests
                    var methodBody = Regex.Replace(
                        method.Body.ToString()
                            .Replace("\n", "")
                            .Replace("\t", "")
                            .Replace("{ ", "{\n")
                            .Replace(" }", "\n}")
                            .Replace("return ", returnType == "Task" ? "" : "return "),
                        @"\s*\)\s*;",
                        httpRequestType == "Get" ? ");" : ", clientId: Context.ConnectionId);"
                    );

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

        private static void VerifyControllerClassName(List<MemberDeclarationSyntax> members)
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

        private static (bool, string, string) GetMethodNameAndAuthorization(MethodDeclarationSyntax method,
            string controllerName)
        {
            var validPathAttrs = new List<string> { "HttpGet", "HttpPost", "HttpPut", "HttpDelete" };
            var httpRequestType = "";
            var httpPath = "";
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
                        httpPath = string.Join(
                            "",
                            argument.ToString()
                                .Replace("[controller]", controllerName)
                                .Replace("[action]", method.Identifier.Text)
                                .Replace("{", "")
                                .Replace("}", "")
                                .Replace("\"", "")
                                .Split("/")
                                .Select(x => x.ToPascalCase())
                        );

                        // Reject any routes using token replacement with tokens other than [controller] or [action].
                        if (httpPath.Contains("[")
                            || httpPath.Contains("]"))
                            throw new Exception(
                                $"The SQE API only allows paths using token replacement with the [controller] and [action] tokens. The method {method.Identifier.Text} violates this policy."
                            );
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

        private static void WriteHubController(List<ControllerField> fields, string projectRoot, string hubFolder)
        {
            var template = new StreamReader($"{projectRoot}/HubConstructorTemplate.txt").ReadToEnd();
            template = template
                .Replace(
                    "$Fields",
                    string.Join("\n", fields.Select(x => $"\t\t{x.modifiers} {x.type} {x.name};"))
                )
                .Replace("$Params", string.Join(", ", fields.Select(x => $"{x.type} {x.name.Replace("_", "")}")))
                .Replace(
                    "$Body",
                    string.Join("\n", fields.Select(x => $"\t\t\t{x.name} = {x.name.Replace("_", "")};"))
                );
            using (var outputFile = new StreamWriter($"{hubFolder}/HubConstructor.cs"))
            {
                outputFile.Write(_autogenFileDisclaimer);
                outputFile.Write(template);
            }
        }

        private static void CopySubscriptionHubController(string projectRoot, string hubFolder)
        {
            var subscriptionHub = new StreamReader(Path.Combine(projectRoot, "SubscriptionHub.cs.txt")).ReadToEnd();
            using (var outputFile = new StreamWriter(Path.Combine(hubFolder, "SubscriptionHub.cs")))
            {
                outputFile.Write(_subscriptionHubDisclaimer);
                outputFile.Write(subscriptionHub);
            }
        }

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
    }
}