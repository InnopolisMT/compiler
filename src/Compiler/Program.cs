using Compiler.Lexer;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Default file path (relative to current working directory)
            string filePath = "examples/test.imperative";

            // Parse command line arguments
            if (args.Length > 0)
            {
                if (args[0] == "--help" || args[0] == "-h")
                {
                    PrintHelp();
                    return;
                }
                filePath = args[0];
            }

            // Check if file exists
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Error: File '{filePath}' not found.");
                Console.Error.WriteLine("Use --help for usage information.");
                Environment.Exit(1);
            }

            try
            {
                // Read and tokenize the file
                string input = File.ReadAllText(filePath);
                Console.WriteLine($"Compiling: {filePath}");
                Console.WriteLine(new string('=', 50));

                var lexer = new LexerClass(input);
                var tokens = lexer.NextToken();

                while (tokens.Type != TokenType.tkEOF)
                {
                    Console.WriteLine(tokens);
                    tokens = lexer.NextToken();
                }

                Console.WriteLine(new string('=', 50));
                Console.WriteLine("Compilation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("Compiler - Imperative Language Compiler");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  Compiler [file_path]");
            Console.WriteLine("  Compiler --help");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  file_path    Path to the source file to compile (relative to current directory)");
            Console.WriteLine("               (default: examples/test.imperative)");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h   Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Compiler                                   # Compile default file (examples/test.imperative)");
            Console.WriteLine("  Compiler examples/simple.imperative        # Compile specific file (relative path)");
            Console.WriteLine("  Compiler /absolute/path/to/file.imperative # Compile with absolute path");
        }
    }
}