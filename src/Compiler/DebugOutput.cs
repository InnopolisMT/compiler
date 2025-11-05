using Compiler.AST;
using Compiler.Lexer;
using Compiler.TreeView;
using System.Text;

namespace Compiler
{
    public static class DebugOutput
    {
        public static void PrintLexicalAnalysis(LexerClass lexer)
        {
            Console.WriteLine("LEXICAL ANALYSIS:");
            var token = lexer.NextToken();
            while (token.Type != TokenType.tkEOF)
            {
                Console.WriteLine(token);
                token = lexer.NextToken();
            }
        }

        public static void GenerateAndPrintDebugInfo(string input, string filePath, ProgramNode ast)
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

        public static void PrintDetailedAst(ProgramNode program)
        {
            Console.WriteLine("ProgramNode");
            Console.WriteLine($"  Declarations: [{program.Declarations.Count}]");

            for (int i = 0; i < program.Declarations.Count; i++)
            {
                Console.WriteLine($"\n  [{i}] {program.Declarations[i].GetType().Name}");
                PrintDeclaration(program.Declarations[i], "    ");
            }
        }

        private static void PrintDeclaration(DeclarationNode decl, string indent)
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
                    if (routineDecl.Body != null)
                    {
                        Console.WriteLine($"{indent}Body:");
                        PrintBody(routineDecl.Body, indent + "  ");
                    }
                    else
                    {
                        Console.WriteLine($"{indent}Body: (forward declaration)");
                    }
                    break;
            }
        }

        private static void PrintType(TypeNode type, string indent)
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

        private static void PrintBody(BodyNode body, string indent)
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

        private static void PrintStatement(StatementNode stmt, string indent)
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

        private static void PrintExpression(ExpressionNode expr, string indent)
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
    }
}

