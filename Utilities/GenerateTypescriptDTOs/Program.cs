using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TypeScriptModelsGenerator;
using System.Linq;

namespace SQE.Utilities.GenerateTypescriptDTOs
{
    static class Program
    {
        static string InputAssembly;
        static string OutputFile;

        static void Main(string[] args)
        {
            Console.WriteLine("Generate Typescript DTOs");
            if (!ParseArguments(args))
                return;

            // Generate the TypeScript definitions
            TypeScriptGenerationResult result;  // Not using var so we can F12 on the type and see its properties
            TypeScriptModelsGeneration
                .Setup(InputAssembly, (options) =>
                {
                    options.Log = Console.WriteLine;
                    options.VerboseLogging = true;
                    options.GenerationMode = TypeScriptModelsGenerator.Options.GenerationMode.Strict;
                })
                .Execute(out result);

            // Combine all files into one big TypeScript file
            using (var sw = new StreamWriter(new FileStream(OutputFile, FileMode.Create), Encoding.UTF8))
            {
                foreach (var file in result.Files)
                {
                    // Each file contains import statements, we want to remove them, as we're putting everything in one file
                    var noImports = RemoveImports(file.Content);
                    sw.Write(noImports);
                }
            }

            Console.WriteLine($"DTOs written to ${OutputFile}");
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
            catch(Exception e)
            {
                Console.Error.WriteLine("Invalid arguments: " + e.Message);
                Console.Error.WriteLine(@"
Usage:
GenerateTypescriptDTOs <sqe-dtos.dll> <output-file.ts>
                                    ");
            }

            return false;
        }

        private static string RemoveImports(string typescript)
        {
            var lines = Regex.Split(typescript, "\r\n|\r|\n"); // https://stackoverflow.com/a/1508217/871910
            var noImports = lines.Where(line => !line.StartsWith("import"));

            return String.Join('\n', noImports);
        }
    }
}
