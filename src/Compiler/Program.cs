using Compiler.Lexer;
using Compiler.Parser;
using Compiler.AST;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Default file path (relative to current working directory)
            string filePath = "examples/test.imperative";
            bool lexerOnly = false;
            bool debugMode = false;

            // Parse command line arguments
            if (args.Length > 0)
            {
                if (args[0] == "--help" || args[0] == "-h")
                {
                    PrintHelp();
                    return;
                }
                if (args[0] == "--lexer-only")
                {
                    lexerOnly = true;
                    if (args.Length > 1)
                        filePath = args[1];
                }
                else if (args[0] == "--debug")
                {
                    debugMode = true;
                    if (args.Length > 1)
                        filePath = args[1];
                }
                else
                {
                    filePath = args[0];
                    // Check for debug flag in second argument
                    if (args.Length > 1 && args[1] == "--debug")
                        debugMode = true;
                }
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
                // Read source file
                string input = File.ReadAllText(filePath);
                Console.WriteLine($"Compiling: {filePath}");
                Console.WriteLine(new string('=', 50));

                if (lexerOnly)
                {
                    // Lexer-only mode: just tokenize and display tokens
                    var lexer = new LexerClass(input);
                    var token = lexer.NextToken();

                    Console.WriteLine("LEXICAL ANALYSIS:");
                    while (token.Type != TokenType.tkEOF)
                    {
                        Console.WriteLine(token);
                        token = lexer.NextToken();
                    }
                }
                else
                {
                    // Full compilation: lexer + parser
                    var lexer = new LexerClass(input);
                    var parser = new ParserFacade(lexer);
                    
                    Console.WriteLine("PARSING...");
                    ProgramNode ast = parser.Parse();
                    
                    Console.WriteLine("✓ Parsing completed successfully");
                    Console.WriteLine($"✓ AST contains {ast.Declarations.Count} top-level declarations");
                    
                    // Display AST structure
                    if (debugMode)
                    {
                        Console.WriteLine("\n=== DETAILED AST (DEBUG MODE) ===");
                        PrintDetailedAst(ast);
                    }
                    else
                    {
                        Console.WriteLine("\nAST STRUCTURE:");
                        PrintAst(ast);
                        Console.WriteLine("\n💡 Use --debug flag for detailed AST tree");
                    }
                }

                Console.WriteLine(new string('=', 50));
                Console.WriteLine("Compilation completed successfully.");
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

        static void PrintAst(ProgramNode program, int indent = 0)
        {
            string indentStr = new string(' ', indent * 2);
            
            foreach (var decl in program.Declarations)
            {
                switch (decl)
                {
                    case VariableDeclarationNode varDecl:
                        Console.WriteLine($"{indentStr}Variable: {varDecl.Name} : {GetTypeName(varDecl.Type)}" + 
                            (varDecl.InitialValue != null ? " = <expr>" : ""));
                        break;
                    case TypeDeclarationNode typeDecl:
                        Console.WriteLine($"{indentStr}Type: {typeDecl.Name} = {GetTypeName(typeDecl.Type)}");
                        break;
                    case RoutineDeclarationNode routineDecl:
                        Console.WriteLine($"{indentStr}Routine: {routineDecl.Name}({routineDecl.Parameters.Count} params)" +
                            (routineDecl.ReturnType != null ? $" : {GetTypeName(routineDecl.ReturnType)}" : ""));
                        Console.WriteLine($"{indentStr}  Body: {routineDecl.Body.Declarations.Count} decls, {routineDecl.Body.Statements.Count} stmts");
                        break;
                }
            }
        }

        static string GetTypeName(TypeNode type)
        {
            return type switch
            {
                PrimitiveTypeNode prim => prim.TypeName,
                UserTypeNode user => user.TypeName,
                ArrayTypeNode arr => $"array[...] {GetTypeName(arr.ElementType)}",
                RecordTypeNode rec => $"record({rec.Fields.Count} fields)",
                _ => "unknown"
            };
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
            Console.WriteLine("  Compiler [options] [file_path]");
            Console.WriteLine("  Compiler --help");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  file_path    Path to the source file to compile (relative to current directory)");
            Console.WriteLine("               (default: examples/test.imperative)");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help, -h       Show this help message");
            Console.WriteLine("  --lexer-only     Run only lexical analysis (display tokens)");
            Console.WriteLine("  --debug          Show detailed AST tree structure");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Compiler                                   # Compile default file");
            Console.WriteLine("  Compiler examples/simple.imperative        # Compile specific file");
            Console.WriteLine("  Compiler examples/test.imperative --debug  # Detailed AST output");
            Console.WriteLine("  Compiler --lexer-only examples/test.imperative  # Lexical analysis only");
        }
    }
}