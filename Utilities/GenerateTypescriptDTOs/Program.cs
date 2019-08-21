using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TypeScriptModelsGenerator;
using TypeScriptModelsGenerator.Options;

namespace SQE.Utilities.GenerateTypescriptDTOs
{
	internal static class Program
	{
		private static string InputAssembly;
		private static string OutputFile;

		private static void Main(string[] args)
		{
			Console.WriteLine("Generate Typescript DTOs");
			if (!ParseArguments(args))
				return;

			// Generate the TypeScript definitions
			TypeScriptGenerationResult result; // Not using var so we can F12 on the type and see its properties
			TypeScriptModelsGeneration
				.Setup(
					InputAssembly,
					options =>
					{
						options.Log = Console.WriteLine;
						options.VerboseLogging = true;
						options.GenerationMode = GenerationMode.Strict;
					}
				)
				.Execute(out result);

			// Combine all files into one big TypeScript file
			using (var sw = new StreamWriter(new FileStream(OutputFile, FileMode.Create), Encoding.UTF8))
			{
				sw.WriteLine("// This file was generate automatically. DO NOT EDIT.");
				sw.WriteLine("/* tslint:disable */");
				foreach (var file in result.Files)
				{
					var processed = PostprocessTypescript(file.Content);
					sw.Write(processed);
				}
			}

			Console.WriteLine($"DTOs written to {OutputFile}");
		}

		private static bool ParseArguments(string[] args)
		{
			try
			{
				if (args.Length != 2)
					throw new InvalidOperationException("Expected two arguments");
				InputAssembly = args[0];
				OutputFile = args[1];
				return true;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Invalid arguments: " + e.Message);
				Console.Error.WriteLine(
					@"
Usage:
GenerateTypescriptDTOs <sqe-dtos.dll> <output-file.ts>
                                    "
				);
			}

			return false;
		}

		private static string PostprocessTypescript(string typescript)
		{
			// We need to perform some post processing on the typescript files.
			// This is done on a line level, so:

			// Break into lines
			var lines = Regex.Split(typescript, "\r\n|\r|\n"); // https://stackoverflow.com/a/1508217/871910

			// Remove all import statements
			var noImports = lines.Where(line => !line.StartsWith("import"));

			// Convert classes into interfaces
			var withInterfaces = noImports.Select(line => line.Replace("class", "interface"));

			// Fix various issues with the definitions
			var fixedDefinitions = withInterfaces.Select(line => FixDefinition(line));

			return string.Join('\n', fixedDefinitions);
		}

		private static string FixDefinition(string line)
		{
			// There are several issues with the definitions, when the property type is not a primitive type.
			// All non primitive properties are translate into:
			//     prop = new type();
			// instead of
			//     prop: type;
			//
			// In addition, a C# List<T> translates into Array<T> an not T[]
			// A C#'s Dictionary<k, v> translates into Map<T, V> instead of { [key: k]: v }
			var match = Regex.Match(line, @"^(?<indent>\s*)(?<name>.+)\s=\snew\s(?<type>.*)\(\);$");
			if (!match.Success)
				return line;

			var type = FixType(match.Groups["type"].Value);
			var correct = $"{match.Groups["indent"]}{match.Groups["name"]}: {type};";
			return correct;
		}

		private static string FixType(string type)
		{
			// Dates are actually strings that we parse on the Typescript side
			if (type == "Date")
				return "string";

			// Array<T> is T[]
			var matchArray = Regex.Match(type, @"Array<(?<type>.+)>");
			if (matchArray.Success)
				return $"{matchArray.Groups["type"]}[]";

			// Map<k, v> is { [key: k]: v }
			var matchMap = Regex.Match(type, @"Map<(?<key>.+), (?<value>.+)>");
			if (matchMap.Success)
				// Adding { to $"" strings in C# is ugly, so we break the string into three parts
				return "{ " + $"[key: {matchMap.Groups["key"]}] : {matchMap.Groups["value"]}" + " }";
			return type;
		}
	}
}