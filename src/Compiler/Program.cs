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

            string filePath = "";
            bool lexerOnly = false;
            bool debugMode = false;
            bool hasInvalid = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return;
                    case "--lexer-only":
                        lexerOnly = true;
                        break;
                    case "--debug":
                        debugMode = true;
                        break;
                    default:
                        if (string.IsNullOrEmpty(filePath))
                            filePath = args[i];
                        break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Console.Error.WriteLine("Error: File path is required.");
                PrintHelp();
                Environment.Exit(1);
            }

            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Error: File '{filePath}' not found.");
                Environment.Exit(1);
            }

            try
            {
                string input = File.ReadAllText(filePath);

                if (lexerOnly)
                {
                    if (debugMode)
                    {
                        var lexer = new LexerClass(input);
                        DebugOutput.PrintLexicalAnalysis(lexer);
                    }
                    else
                    {
                        var lexer = new LexerClass(input);
                        var token = lexer.NextToken();
                        while (token.Type != TokenType.tkEOF)
                        {
                            if (token.Type == TokenType.tkInvalid)
                            {
                                Console.Error.Write($"{filePath}({token.Span.Line},{token.Span.Start}): ");
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Error.Write($"\x1b[1mInvalid token:\x1b[0m");
                                Console.ResetColor();
                                Console.Error.WriteLine($" '{token.Lexeme}'");
                                hasInvalid = true;
                            }
                            token = lexer.NextToken();
                        }
                        if (hasInvalid)
                        {
                            Console.Error.WriteLine("Lexical analysis completed with errors.");
                            Environment.Exit(1);
                        }
                        Console.WriteLine("Lexical analysis completed successfully.");
                    }
                }
                else
                {
                    var lexer = new LexerClass(input);
                    var token = lexer.NextToken();
                    while (token.Type != TokenType.tkEOF)
                    {
                        if (token.Type == TokenType.tkInvalid)
                        {
                            hasInvalid = true;
                            Console.Error.Write($"{filePath}({token.Span.Line},{token.Span.Start}): ");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.Write($"\x1b[1mInvalid token:\x1b[0m");
                            Console.ResetColor();
                            Console.Error.WriteLine($" '{token.Lexeme}'");
                        }
                        token = lexer.NextToken();
                    }
                    if (hasInvalid)
                    {
                        Console.Error.WriteLine("Errors found during lexical analysis. Aborting parsing.");
                        Environment.Exit(1);
                    }

                    lexer = new LexerClass(input);
                    var parser = new ParserFacade(lexer);
                    ProgramNode ast = parser.Parse();

                    if (debugMode)
                    {
                        DebugOutput.GenerateAndPrintDebugInfo(input, filePath, ast);
                    }
                    else
                    {
                        Console.WriteLine("Compilation completed successfully.");
                    }
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

        static void PrintHelp()
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
}