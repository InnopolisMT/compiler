using Compiler.Lexer;
using Compiler.Parser;
using Compiler.AST;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var options = ParseArguments(args);
            if (options == null)
            {
                return;
            }

            if (!ValidateFilePath(options.FilePath))
            {
                Environment.Exit(1);
            }

            try
            {
                string input = File.ReadAllText(options.FilePath);

                if (options.LexerOnly)
                {
                    RunLexerOnly(input, options);
                }
                else
                {
                    RunFullCompilation(input, options);
                }
            }
            catch (ParseException ex)
            {
                Console.Error.WriteLine($"Parse Error: {ex.Message}");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        private static CompilerOptions? ParseArguments(string[] args)
        {
            string filePath = "";
            bool lexerOnly = false;
            bool debugMode = false;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return null;
                    case "--lexer-only":
                        lexerOnly = true;
                        break;
                    case "--debug":
                        debugMode = true;
                        break;
                    default:
                        if (string.IsNullOrEmpty(filePath))
                        {
                            filePath = arg;
                        }
                        break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Console.Error.WriteLine("Error: File path is required.");
                PrintHelp();
                return null;
            }

            return new CompilerOptions(filePath, lexerOnly, debugMode);
        }

        private static bool ValidateFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Error: File '{filePath}' not found.");
                return false;
            }
            return true;
        }

        private static void RunLexerOnly(string input, CompilerOptions options)
        {
            if (options.DebugMode)
            {
                var lexer = new LexerClass(input);
                DebugOutput.PrintLexicalAnalysis(lexer);
            }
            else
            {
                var lexer = new LexerClass(input);
                bool hasErrors = ValidateLexicalAnalysis(lexer, options.FilePath);
                if (hasErrors)
                {
                    Console.Error.WriteLine("Lexical analysis completed with errors.");
                    Environment.Exit(1);
                }
                Console.WriteLine("Lexical analysis completed successfully.");
            }
        }

        private static void RunFullCompilation(string input, CompilerOptions options)
        {
            var lexer = new LexerClass(input);
            bool hasErrors = ValidateLexicalAnalysis(lexer, options.FilePath);
            if (hasErrors)
            {
                Console.Error.WriteLine("Errors found during lexical analysis. Aborting parsing.");
                Environment.Exit(1);
            }

            lexer = new LexerClass(input);
            var parser = new ParserFacade(lexer);
            ProgramNode ast = parser.Parse();

            if (options.DebugMode)
            {
                DebugOutput.GenerateAndPrintDebugInfo(input, options.FilePath, ast);
            }
            else
            {
                Console.WriteLine("Compilation completed successfully.");
            }
        }

        private static bool ValidateLexicalAnalysis(LexerClass lexer, string filePath)
        {
            bool hasInvalid = false;
            var token = lexer.NextToken();

            while (token.Type != TokenType.tkEOF)
            {
                if (token.Type == TokenType.tkInvalid)
                {
                    PrintInvalidTokenError(token, filePath);
                    hasInvalid = true;
                }
                token = lexer.NextToken();
            }

            return hasInvalid;
        }

        private static void PrintInvalidTokenError(Token token, string filePath)
        {
            Console.Error.Write($"{filePath}({token.Span.Line},{token.Span.Start}): ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write($"\x1b[1mInvalid token:\x1b[0m");
            Console.ResetColor();
            Console.Error.WriteLine($" '{token.Lexeme}'");
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Compiler - Imperative Language Compiler");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  Compiler <file_path> [options]");
            Console.WriteLine("  Compiler --help");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  file_path    Path to the source file to compile");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h       Show this help message");
            Console.WriteLine("  --lexer-only     Run only lexical analysis");
            Console.WriteLine("  --debug          Show detailed output (tokens + AST)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Compiler examples/test.imperative                    # Basic compilation");
            Console.WriteLine("  Compiler examples/test.imperative --debug            # Detailed output");
            Console.WriteLine("  Compiler --lexer-only examples/test.imperative       # Lexical analysis only");
        }
    }

    internal class CompilerOptions
    {
        public string FilePath { get; }
        public bool LexerOnly { get; }
        public bool DebugMode { get; }

        public CompilerOptions(string filePath, bool lexerOnly, bool debugMode)
        {
            FilePath = filePath;
            LexerOnly = lexerOnly;
            DebugMode = debugMode;
        }
    }
}