using Compiler.Lexer;
using Compiler.Parser;
using Compiler.AST;
using Compiler.Optimization;
using Compiler.Semantic;
using Compiler.CodeGen;

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
            string? outputPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
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
                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length)
                        {
                            outputPath = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: --output requires a path argument.");
                            return null;
                        }
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

            return new CompilerOptions(filePath, lexerOnly, debugMode, outputPath);
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
            if (options.DebugMode)
            {
                DebugOutput.PrintLexicalAnalysis(lexer);
                Console.WriteLine();
            }

            lexer = new LexerClass(input);
            var parser = new ParserFacade(lexer);
            ProgramNode ast = parser.Parse();

            if (options.DebugMode)
            {
                Console.WriteLine("AST after parsing:");
                DebugOutput.PrintDetailedAst(ast);
                Console.WriteLine();
            }

            var semanticAnalyzer = new SemanticAnalyzer();
            semanticAnalyzer.Analyze(ast);
            if (semanticAnalyzer.HasErrors)
            {
                foreach (var error in semanticAnalyzer.Errors)
                {
                    Console.WriteLine(error.Format(options.FilePath));
                }
                Environment.Exit(1);
            }

            var constantFolder = new ConstantFolder();
            constantFolder.Optimize(ast);

            if (options.DebugMode)
            {                
                Console.WriteLine("Optimized AST:");
                DebugOutput.PrintDetailedAst(ast);
                Console.WriteLine();
            }

            string outputPath = options.OutputPath ?? Path.ChangeExtension(options.FilePath, ".wasm");
            
            try
            {
                var codeGenerator = new WasmCodeGenerator();
                codeGenerator.Generate(ast, outputPath);
                
                Console.WriteLine($"Compilation completed successfully!");
                Console.WriteLine($"Output files:");
                Console.WriteLine($"  WASM: {outputPath}");
                
                string baseName = Path.GetFileNameWithoutExtension(outputPath);
                string directory = Path.GetDirectoryName(outputPath) ?? ".";
                string testScript = Path.Combine(directory, $"{baseName}_test.js");
                Console.WriteLine($"  Test: {testScript}");
                Console.WriteLine();
                Console.WriteLine($"Run with: node {testScript} [function_name] [args...]");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Code generation failed: {ex.Message}");
                if (options.DebugMode)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                Environment.Exit(1);
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
            Console.WriteLine("  file_path              Path to the source file to compile");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h             Show this help message");
            Console.WriteLine("  --output, -o <path>    Specify output path for WASM file (default: input.wasm)");
            Console.WriteLine("  --debug                Show detailed output (tokens + AST)");
            Console.WriteLine("  --lexer-only           Run only lexical analysis (stop after lexer)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Compiler examples/test.imperative                    # Full compilation to WASM");
            Console.WriteLine("  Compiler examples/test.imperative -o out/prog.wasm   # Custom output path");
            Console.WriteLine("  Compiler examples/test.imperative --debug            # Detailed output");
            Console.WriteLine("  Compiler --lexer-only examples/test.imperative       # Lexical analysis only");
            Console.WriteLine();
            Console.WriteLine("Note: By default, the compiler performs full compilation including code generation.");
            Console.WriteLine("      Use --lexer-only to stop at a specific stage.");
        }
    }

    internal class CompilerOptions(string filePath, bool lexerOnly, bool debugMode, string? outputPath)
    {
        public string FilePath { get; } = filePath;
        public bool LexerOnly { get; } = lexerOnly;
        public bool DebugMode { get; } = debugMode;
        public string? OutputPath { get; } = outputPath;
    }
}