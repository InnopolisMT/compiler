using Compiler.Lexer;
using Compiler.Parser;
using Compiler.AST;
using Compiler.TreeView;
using System.Text;

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

            // Parse command line arguments
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
                    var lexer = new LexerClass(input);
                    var token = lexer.NextToken();

                    if (debugMode)
                    {
                        Console.WriteLine("LEXICAL ANALYSIS:");
                        while (token.Type != TokenType.tkEOF)
                        {
                            Console.WriteLine(token);
                            token = lexer.NextToken();
                        }
                    }
                    else
                    {
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
                        var visualizer = new TreeVisualizer();
                        string html = visualizer.GenerateTreeHtml(ast, filePath);
                        File.WriteAllText("ast_visualization.html", html, Encoding.UTF8);

                        Console.WriteLine("LEXICAL ANALYSIS:");
                        var debugLexer = new LexerClass(input);
                        var debugToken = debugLexer.NextToken();
                        while (debugToken.Type != TokenType.tkEOF)
                        {
                            Console.WriteLine(debugToken);
                            debugToken = debugLexer.NextToken();
                        }

                        Console.WriteLine("\n=== DETAILED AST (DEBUG MODE) ===");
                        PrintDetailedAst(ast);
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


        static void PrintDetailedAst(ProgramNode program)
        {
            Console.WriteLine("ProgramNode");
            Console.WriteLine($"  Declarations: [{program.Declarations.Count}]");
            
            for (int i = 0; i < program.Declarations.Count; i++)
            {
                Console.WriteLine($"\n  [{i}] {program.Declarations[i].GetType().Name}");
                PrintDeclaration(program.Declarations[i], "    ");
            }
        }

        static void PrintDeclaration(DeclarationNode decl, string indent)
        {
            switch (decl)
            {
                case VariableDeclarationNode varDecl:
                    Console.WriteLine($"{indent}Name: \"{varDecl.Name}\"");
                    Console.WriteLine($"{indent}Type:");
                    PrintType(varDecl.Type, indent + "  ");
                    if (varDecl.InitialValue != null)
                    {
                        Console.WriteLine($"{indent}InitialValue:");
                        PrintExpression(varDecl.InitialValue, indent + "  ");
                    }
                    break;

                case TypeDeclarationNode typeDecl:
                    Console.WriteLine($"{indent}Name: \"{typeDecl.Name}\"");
                    Console.WriteLine($"{indent}Type:");
                    PrintType(typeDecl.Type, indent + "  ");
                    break;

                case RoutineDeclarationNode routineDecl:
                    Console.WriteLine($"{indent}Name: \"{routineDecl.Name}\"");
                    Console.WriteLine($"{indent}Parameters: [{routineDecl.Parameters.Count}]");
                    for (int i = 0; i < routineDecl.Parameters.Count; i++)
                    {
                        Console.WriteLine($"{indent}  [{i}] Parameter");
                        Console.WriteLine($"{indent}    Name: \"{routineDecl.Parameters[i].Name}\"");
                        Console.WriteLine($"{indent}    Type:");
                        PrintType(routineDecl.Parameters[i].Type, indent + "      ");
                    }
                    if (routineDecl.ReturnType != null)
                    {
                        Console.WriteLine($"{indent}ReturnType:");
                        PrintType(routineDecl.ReturnType, indent + "  ");
                    }
                    Console.WriteLine($"{indent}Body:");
                    PrintBody(routineDecl.Body, indent + "  ");
                    break;
            }
        }

        static void PrintType(TypeNode type, string indent)
        {
            Console.WriteLine($"{indent}{type.GetType().Name}");
            switch (type)
            {
                case PrimitiveTypeNode prim:
                    Console.WriteLine($"{indent}  TypeName: \"{prim.TypeName}\"");
                    break;

                case UserTypeNode user:
                    Console.WriteLine($"{indent}  TypeName: \"{user.TypeName}\"");
                    break;

                case ArrayTypeNode arr:
                    Console.WriteLine($"{indent}  Size:");
                    PrintExpression(arr.Size, indent + "    ");
                    Console.WriteLine($"{indent}  ElementType:");
                    PrintType(arr.ElementType, indent + "    ");
                    break;

                case RecordTypeNode rec:
                    Console.WriteLine($"{indent}  Fields: [{rec.Fields.Count}]");
                    for (int i = 0; i < rec.Fields.Count; i++)
                    {
                        Console.WriteLine($"{indent}    [{i}] VariableDeclarationNode");
                        PrintDeclaration(rec.Fields[i], indent + "      ");
                    }
                    break;
            }
        }

        static void PrintBody(BodyNode body, string indent)
        {
            Console.WriteLine($"{indent}Declarations: [{body.Declarations.Count}]");
            for (int i = 0; i < body.Declarations.Count; i++)
            {
                Console.WriteLine($"{indent}  [{i}] {body.Declarations[i].GetType().Name}");
                PrintDeclaration(body.Declarations[i], indent + "    ");
            }
            
            Console.WriteLine($"{indent}Statements: [{body.Statements.Count}]");
            for (int i = 0; i < body.Statements.Count; i++)
            {
                Console.WriteLine($"{indent}  [{i}] {body.Statements[i].GetType().Name}");
                PrintStatement(body.Statements[i], indent + "    ");
            }
        }

        static void PrintStatement(StatementNode stmt, string indent)
        {
            switch (stmt)
            {
                case AssignmentNode assign:
                    Console.WriteLine($"{indent}Target:");
                    PrintExpression(assign.Target, indent + "  ");
                    Console.WriteLine($"{indent}Value:");
                    PrintExpression(assign.Value, indent + "  ");
                    break;

                case IfStatementNode ifStmt:
                    Console.WriteLine($"{indent}Condition:");
                    PrintExpression(ifStmt.Condition, indent + "  ");
                    Console.WriteLine($"{indent}ThenBody: [{ifStmt.ThenBody.Count} statements]");
                    for (int i = 0; i < ifStmt.ThenBody.Count; i++)
                    {
                        Console.WriteLine($"{indent}  [{i}] {ifStmt.ThenBody[i].GetType().Name}");
                        PrintStatement(ifStmt.ThenBody[i], indent + "    ");
                    }
                    if (ifStmt.ElseBody.Count > 0)
                    {
                        Console.WriteLine($"{indent}ElseBody: [{ifStmt.ElseBody.Count} statements]");
                        for (int i = 0; i < ifStmt.ElseBody.Count; i++)
                        {
                            Console.WriteLine($"{indent}  [{i}] {ifStmt.ElseBody[i].GetType().Name}");
                            PrintStatement(ifStmt.ElseBody[i], indent + "    ");
                        }
                    }
                    break;

                case WhileLoopNode whileLoop:
                    Console.WriteLine($"{indent}Condition:");
                    PrintExpression(whileLoop.Condition, indent + "  ");
                    Console.WriteLine($"{indent}Body: [{whileLoop.Body.Count} statements]");
                    for (int i = 0; i < whileLoop.Body.Count; i++)
                    {
                        Console.WriteLine($"{indent}  [{i}] {whileLoop.Body[i].GetType().Name}");
                        PrintStatement(whileLoop.Body[i], indent + "    ");
                    }
                    break;

                case ForLoopNode forLoop:
                    Console.WriteLine($"{indent}Variable: \"{forLoop.Variable}\"");
                    Console.WriteLine($"{indent}IsReverse: {forLoop.IsReverse}");
                    Console.WriteLine($"{indent}Range:");
                    PrintExpression(forLoop.Range, indent + "  ");
                    Console.WriteLine($"{indent}Body: [{forLoop.Body.Count} statements]");
                    for (int i = 0; i < forLoop.Body.Count; i++)
                    {
                        Console.WriteLine($"{indent}  [{i}] {forLoop.Body[i].GetType().Name}");
                        PrintStatement(forLoop.Body[i], indent + "    ");
                    }
                    break;

                case ReturnStatementNode retStmt:
                    if (retStmt.Value != null)
                    {
                        Console.WriteLine($"{indent}Value:");
                        PrintExpression(retStmt.Value, indent + "  ");
                    }
                    else
                    {
                        Console.WriteLine($"{indent}(void return)");
                    }
                    break;

                case PrintStatementNode printStmt:
                    Console.WriteLine($"{indent}Expression:");
                    PrintExpression(printStmt.Expression, indent + "  ");
                    break;
            }
        }

        static void PrintExpression(ExpressionNode expr, string indent)
        {
            Console.WriteLine($"{indent}{expr.GetType().Name}");
            switch (expr)
            {
                case IntegerLiteralNode intLit:
                    Console.WriteLine($"{indent}  Value: {intLit.Value}");
                    break;

                case RealLiteralNode realLit:
                    Console.WriteLine($"{indent}  Value: {realLit.Value}");
                    break;

                case BooleanLiteralNode boolLit:
                    Console.WriteLine($"{indent}  Value: {boolLit.Value}");
                    break;

                case IdentifierNode id:
                    Console.WriteLine($"{indent}  Name: \"{id.Name}\"");
                    break;

                case BinaryOperationNode binOp:
                    Console.WriteLine($"{indent}  Operator: \"{binOp.Operator}\"");
                    Console.WriteLine($"{indent}  Left:");
                    PrintExpression(binOp.Left, indent + "    ");
                    Console.WriteLine($"{indent}  Right:");
                    PrintExpression(binOp.Right, indent + "    ");
                    break;

                case UnaryOperationNode unOp:
                    Console.WriteLine($"{indent}  Operator: \"{unOp.Operator}\"");
                    Console.WriteLine($"{indent}  Operand:");
                    PrintExpression(unOp.Operand, indent + "    ");
                    break;

                case ArrayAccessNode arrAccess:
                    Console.WriteLine($"{indent}  Array:");
                    PrintExpression(arrAccess.Array, indent + "    ");
                    Console.WriteLine($"{indent}  Index:");
                    PrintExpression(arrAccess.Index, indent + "    ");
                    break;

                case RecordAccessNode recAccess:
                    Console.WriteLine($"{indent}  FieldName: \"{recAccess.FieldName}\"");
                    Console.WriteLine($"{indent}  Record:");
                    PrintExpression(recAccess.Record, indent + "    ");
                    break;

                case RoutineCallNode call:
                    Console.WriteLine($"{indent}  RoutineName: \"{call.RoutineName}\"");
                    Console.WriteLine($"{indent}  Arguments: [{call.Arguments.Count}]");
                    for (int i = 0; i < call.Arguments.Count; i++)
                    {
                        Console.WriteLine($"{indent}    [{i}]");
                        PrintExpression(call.Arguments[i], indent + "      ");
                    }
                    break;

                case RangeNode range:
                    Console.WriteLine($"{indent}  Start:");
                    PrintExpression(range.Start, indent + "    ");
                    Console.WriteLine($"{indent}  End:");
                    PrintExpression(range.End, indent + "    ");
                    break;

                case ArrayInitializerNode arrInit:
                    Console.WriteLine($"{indent}  Elements: [{arrInit.Elements.Count}]");
                    for (int i = 0; i < arrInit.Elements.Count; i++)
                    {
                        Console.WriteLine($"{indent}    [{i}]");
                        PrintExpression(arrInit.Elements[i], indent + "      ");
                    }
                    break;
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