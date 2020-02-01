﻿/*
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using CaseExtensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerateTypescriptInterfaces
{
    internal static class Program
    {
        private const string _hubClassTemplate = @"namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub
    {";

        private const string _autogenFileDisclaimer = @"/*
 * Do not edit this file directly!
 * This SignalRSQE class is autogenerated by the `GenerateTypescriptInterfaces` 
 * in the project https://github.com/Scripta-Qumranica-Electronica/SQE_API.
 * Changes made there are used to automatically create this file at {ROOT}/ts-dtos
 * whenever the GenerateTypescriptInterfaces program is run.
 */

";

        private const string _serverMethodTemplate = @"
    $COMMENT
    public async $METHODNAMELC($METHODPARAMS): Promise<$METHODRETURN> {
        return await this._connection!.invoke($METHODNAME);
    }";

        private const string _clientMethodTemplate = @"
    $ONCOMMENT
    public on$METHODNAME(func: ($METHODRETURN) => void): void {
        this._connection!.on('$METHODNAME', func)
    }

    $OFFCOMMENT
    public off$METHODNAME(func: ($METHODRETURN) => void): void {
        this._connection!.off('$METHODNAME', func)
    }
";

        private const string _onListener = "/**\n\t * Add a listener for when the server";

        private const string _offListener = "/**\n\t * Remove an existing listener that triggers when the server";

        private static readonly Regex _rx = new Regex(
            @"Task?<(?<return>.*)?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex _rxl = new Regex(
            @"List?<(?<return>.*)?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex _rxi = new Regex(
            @"interface (?<type>.*?) ",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static void Main(string[] args)
        {
            ParseSqeSignalrHub();
        }

        private static void ParseSqeSignalrHub()
        {
            Console.WriteLine("Parsing the SignalR hub and creating corresponding typescript interfaces.");

            // TODO: Can we find a better way to resolve these paths instead of all the backtracking?
            var projectRoot =
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../", "../", "../"));
            var apiServerRoot = Path.GetFullPath(Path.Combine(projectRoot, "../", "../", "sqe-api-server"));
            var tsFolder = Path.GetFullPath(Path.Combine(apiServerRoot, "../", "ts-dtos"));
            var hubFolder = Path.GetFullPath(Path.Combine(apiServerRoot, "RealtimeHubs"));

            var dir = new DirectoryInfo(hubFolder);
            var files = dir.GetFiles("*.cs");
            var hubMethods = new List<MethodDesc>();
            var hubInterfaceMethods = new List<MethodDesc>();

            // Process the Realtime Hub methods
            Console.WriteLine($"Processing realtime hub methods from {hubFolder}.");
            foreach (var file in files)
            {
                if (file.Name != "HubInterface.cs")
                {
                    var tmpHubMethods = ParseSqeHttpControllers(file);
                    hubMethods = hubMethods.Union(tmpHubMethods).ToList();
                }
                else
                    hubInterfaceMethods = ParseSqeHttpControllers(file);
            }

            WriteTsHub(hubMethods, hubInterfaceMethods, tsFolder, projectRoot);
            Console.WriteLine("Finished creating the SQE signalr typescript interface.");
        }

        private static List<MethodDesc> ParseSqeHttpControllers(FileInfo file)
        {
            // Parse the code to the relevant members
            var code = new StreamReader(file.FullName).ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var members = root.DescendantNodes().OfType<MemberDeclarationSyntax>().ToList();
            var hubMethods = new List<MethodDesc>();

            // Parse individual methods
            foreach (var method in members.OfType<MethodDeclarationSyntax>().ToList())
            {
                // Copy the comments
                var comments = method.GetLeadingTrivia().ToString().Trim();
                if (!string.IsNullOrEmpty(comments))
                    comments = ParseComments(comments);

                var methodName = method.Identifier.Text;
                //Console.WriteLine(methodName);

                // Format the method modifiers
                var methodModifiers = string.Join(" ", method.Modifiers.Select(x => x.Text));
                //Console.WriteLine(methodModifiers);

                // Format the return type
                var returnType = ConvertToTypescriptType(ProcessReturnType(method));

                //Console.WriteLine(returnType);

                // Format the method parameters
                var methodParams = string.Join(
                    ", ",
                    method.ParameterList.Parameters.Select(
                        x => $"{x.Identifier}: {ConvertToTypescriptType(x.Type.ToString())}"
                    )
                );
                //Console.WriteLine(methodParams);
                var typescriptSig = $"\t\t{methodName}({methodParams}): {returnType}";
                hubMethods.Add(new MethodDesc(methodName, methodParams, returnType, comments));
            }
            return hubMethods;
        }

        private static string ParseComments(string comment)
        {
            var xDoc = new XmlDocument();
            xDoc.LoadXml($"<root>{comment.Replace("///", "")}</root>");

            // Get the summary text for the method
            var summary = xDoc.GetElementsByTagName("summary");
            var summaryText = "";
            if (summary.Count == 1)
                summaryText = "\n " + Regex.Replace(summary[0].InnerText, @" +", " ").Trim() + "\n";

            // Get the comments for the parameters
            var parameters = xDoc.GetElementsByTagName("param");
            var parameterText = string.Join("\n",
                parameters.Cast<XmlNode>().Select(x => $" @param {x.Attributes["name"].Value} - {x.InnerText}"));

            // Get the comments for the returns
            var returns = xDoc.GetElementsByTagName("returns");
            var returnText = "";
            if (returns.Count == 1)
                returnText = returns[0].InnerText;

            return $"/**{summaryText}\n{parameterText}\n{(!string.IsNullOrEmpty(returnText) ? " @returns - " + returnText : "")}\n/".Replace("\n", "\n\t *");
        }

        private static string ConvertToTypescriptType(string type)
        {
            switch (type)
            {
                case "byte":
                case "sbyte":
                case "uint16":
                case "uint32":
                case "uint64":
                case "int16":
                case "int32":
                case "int64":
                case "decimal":
                case "double":
                case "single":
                case "uint":
                case "int":
                    return "number";
                case "string":
                    return "string";
                case "bool":
                    return "boolean";
            }
            if (type != null && type.Contains("Task"))
            {
                var matches = _rx.Matches(type);
                type = matches.Count >= 1 ? ConvertToTypescriptType(matches[0].Groups["return"].Value) : "void";
            }
            if (type != null && type.Contains("List<"))
            {
                var matches = _rxl.Matches(type);
                type = matches.Count >= 1 ? ConvertToTypescriptType(matches[0].Groups["return"].Value) + "[]" : "[]";
            }

            return type;
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

        private static void WriteTsHub(List<MethodDesc> hubMethods, List<MethodDesc> hubInterfaceMethods, string tsFolder, string baseFolder)
        {
            var rgx = new Regex(@"(\w+?):");
            var types = new StreamReader($"{tsFolder}/sqe-dtos.ts").ReadToEnd();
            var matches = _rxi.Matches(types);
            var template = new StreamReader($"{baseFolder}/SqeHubInterfaceTemplate.txt").ReadToEnd();
            using (var outputFile = new StreamWriter(Path.Combine(tsFolder, "sqe-signalr.ts")))
            {
                outputFile.Write(_autogenFileDisclaimer);
                outputFile.Write(template
                    .Replace("$IMPORTS", string.Join("\n", matches.ToList().Select(x => $"\t{x.Groups["type"].Value},")))
                    .Replace("$CLIENTMETHODS", string.Join("\n", hubInterfaceMethods.Select(x =>
                        _clientMethodTemplate.Replace("$ONCOMMENT", string.IsNullOrEmpty(x.comment) ? "" : x.comment.Replace("/**\n\t *", _onListener))
                            .Replace("$OFFCOMMENT", string.IsNullOrEmpty(x.comment) ? "" : x.comment.Replace("/**\n\t *", _offListener))
                            .Replace("$METHODNAME", x.name)
                            .Replace("$METHODRETURN", x.parameters))))
                    .Replace("$SERVERMETHODS", string.Join("\n", hubMethods.Select(x =>
                        _serverMethodTemplate.Replace("$COMMENT", x.comment)
                            .Replace("$METHODNAMELC", x.name.ToCamelCase())
                            .Replace("$METHODNAME", $"'{x.name}'" +
                                                    $"{string.Join("", rgx.Matches(x.parameters).ToList().Select(x => ", " + x.Value.Replace(":", "")))}")
                            .Replace("$METHODPARAMS", x.parameters)
                            .Replace("$METHODRETURN", x.returnType))))
                );
            }
        }
    }

    public class MethodDesc
    {
        public MethodDesc(string name, string parameters, string returnType, string comment)
        {
            this.name = name;
            this.parameters = parameters;
            this.returnType = returnType;
            this.comment = comment;
        }

        public string name { get; set; }
        public string parameters { get; set; }
        public string returnType { get; set; }
        public string comment { get; set; }
    }
}