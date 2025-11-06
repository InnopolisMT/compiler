using System;
using System.Collections.Generic;
using System.Text;
using Compiler.AST;
using Compiler.Lexer;
using Compiler.TreeView;

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

        public static void GenerateAndPrintDebugInfo(string input, string filePath, ProgramNode ast, bool printConsole = true)
        {
            var visualizer = new TreeVisualizer();
            string html = visualizer.GenerateTreeHtml(ast, filePath);
            File.WriteAllText("ast_visualization.html", html, Encoding.UTF8);

            if (!printConsole) return;

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
            if (program == null)
            {
                Console.WriteLine("ProgramNode (null)");
                return;
            }

            Console.WriteLine(Ansi.Node("ProgramNode"));

            var properties = new List<(string Label, Action<string>? Child)>
            {
                (FormatCount("Declarations", program.Declarations.Count), program.Declarations.Count > 0
                    ? new Action<string>(indent => PrintDeclarationList(program.Declarations, indent))
                    : null)
            };

            PrintProperties(string.Empty, properties);
        }

        private static class Ansi
        {
            private const string Reset = "\x1b[0m";
            private const string Blue = "\x1b[34m";
            private const string Green = "\x1b[32m";
            private const string Cyan = "\x1b[36m";
            private const string Yellow = "\x1b[33m";
            private const string Faint = "\x1b[2m";

            internal static bool Enabled { get; } = !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") == null;

            internal static string Node(string text) => Enabled ? $"{Blue}{text}{Reset}" : text;
            internal static string Label(string text) => Enabled ? $"{Green}{text}{Reset}" : text;
            internal static string Value(string text) => Enabled ? $"{Cyan}{text}{Reset}" : text;
            internal static string Index(string text) => Enabled ? $"{Yellow}{text}{Reset}" : text;
            internal static string Muted(string text) => Enabled ? $"{Faint}{text}{Reset}" : text;
        }

        private static string FormatLabel(string label) => $"{Ansi.Label(label)}:";

        private static string FormatLabelValue(string label, string value) => $"{Ansi.Label(label)}: {value}";

        private static string FormatCount(string label, int count, string? unit = null)
        {
            string suffix = unit != null ? $"[{count} {unit}]" : $"[{count}]";
            return $"{Ansi.Label(label)} {Ansi.Index(suffix)}";
        }

        private static void PrintTreeLine(string indent, bool isLast, string text)
        {
            Console.Write(indent);
            Console.Write(isLast ? "`- " : "|- ");
            Console.WriteLine(text);
        }

        private static string ExtendIndent(string indent, bool parentIsLast)
        {
            return indent + (parentIsLast ? "   " : "|  ");
        }

        private static void PrintProperties(string indent, IReadOnlyList<(string Label, Action<string>? Child)> properties)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                var (label, child) = properties[i];
                bool isLast = i == properties.Count - 1;
                PrintTreeLine(indent, isLast, label);
                if (child != null)
                {
                    string nextIndent = ExtendIndent(indent, isLast);
                    child(nextIndent);
                }
            }
        }

        private static void PrintDeclarationList(IReadOnlyList<DeclarationNode> declarations, string indent)
        {
            for (int i = 0; i < declarations.Count; i++)
            {
                bool isLast = i == declarations.Count - 1;
                var declaration = declarations[i];
                string header = $"{Ansi.Index($"[{i}]")} {Ansi.Node(declaration.GetType().Name)}";
                PrintDeclaration(declaration, indent, header, isLast);
            }
        }

        private static void PrintDeclaration(DeclarationNode decl, string indent, string header, bool isLast)
        {
            PrintTreeLine(indent, isLast, header);
            string childIndent = ExtendIndent(indent, isLast);

            switch (decl)
            {
                case VariableDeclarationNode varDecl:
                    var variableProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabelValue("Name", Ansi.Value($"\"{varDecl.Name}\"")), null),
                        (FormatLabel("Type"), varDecl.Type != null
                            ? new Action<string>(typeIndent => PrintType(varDecl.Type, typeIndent, true))
                            : new Action<string>(typeIndent => PrintTreeLine(typeIndent, true, Ansi.Muted("(not specified)"))))
                    };

                    if (varDecl.InitialValue != null)
                    {
                        variableProps.Add((FormatLabel("InitialValue"), valueIndent => PrintExpression(varDecl.InitialValue, valueIndent, true)));
                    }

                    PrintProperties(childIndent, variableProps);
                    break;

                case TypeDeclarationNode typeDecl:
                    var typeDeclProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabelValue("Name", Ansi.Value($"\"{typeDecl.Name}\"")), null),
                        (FormatLabel("Type"), typeDecl.Type != null
                            ? new Action<string>(typeIndent => PrintType(typeDecl.Type, typeIndent, true))
                            : new Action<string>(typeIndent => PrintTreeLine(typeIndent, true, Ansi.Muted("(not specified)"))))
                    };

                    PrintProperties(childIndent, typeDeclProps);
                    break;

                case RoutineDeclarationNode routineDecl:
                    var routineProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabelValue("Name", Ansi.Value($"\"{routineDecl.Name}\"")), null),
                        (FormatCount("Parameters", routineDecl.Parameters.Count), routineDecl.Parameters.Count > 0
                            ? new Action<string>(paramIndent => PrintParameterList(routineDecl.Parameters, paramIndent))
                            : null)
                    };

                    if (routineDecl.ReturnType != null)
                    {
                        routineProps.Add((FormatLabel("ReturnType"), typeIndent => PrintType(routineDecl.ReturnType, typeIndent, true)));
                    }

                    routineProps.Add(routineDecl.Body != null
                        ? (FormatLabel("Body"), new Action<string>(bodyIndent => PrintBody(routineDecl.Body, bodyIndent)))
                        : (FormatLabelValue("Body", Ansi.Muted("(forward declaration)")), null));

                    PrintProperties(childIndent, routineProps);
                    break;
            }
        }

        private static void PrintParameterList(IReadOnlyList<ParameterNode> parameters, string indent)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                bool isLast = i == parameters.Count - 1;
                var parameter = parameters[i];
                PrintTreeLine(indent, isLast, $"{Ansi.Index($"[{i}]")} {Ansi.Node("Parameter")}");
                string childIndent = ExtendIndent(indent, isLast);

                var parameterProps = new List<(string, Action<string>?)>
                {
                    (FormatLabelValue("Name", Ansi.Value($"\"{parameter.Name}\"")), null),
                    (FormatLabel("Type"), parameter.Type != null
                        ? new Action<string>(typeIndent => PrintType(parameter.Type, typeIndent, true))
                        : new Action<string>(typeIndent => PrintTreeLine(typeIndent, true, Ansi.Muted("(not specified)"))))
                };

                PrintProperties(childIndent, parameterProps);
            }
        }

        private static void PrintFieldDeclaration(FieldDeclarationNode fieldDecl, string indent)
        {
            var properties = new List<(string, Action<string>?)>
            {
                (FormatLabelValue("Name", Ansi.Value($"\"{fieldDecl.Name}\"")), null),
                (FormatLabel("Type"), fieldDecl.Type != null
                    ? new Action<string>(typeIndent => PrintType(fieldDecl.Type, typeIndent, true))
                    : new Action<string>(typeIndent => PrintTreeLine(typeIndent, true, Ansi.Muted("(not specified)"))))
            };

            PrintProperties(indent, properties);
        }

        private static void PrintBody(BodyNode body, string indent)
        {
            var properties = new List<(string, Action<string>?)>
            {
                (FormatCount("Declarations", body.Declarations.Count), body.Declarations.Count > 0
                    ? new Action<string>(declIndent => PrintDeclarationList(body.Declarations, declIndent))
                    : null),
                (FormatCount("Statements", body.Statements.Count), body.Statements.Count > 0
                    ? new Action<string>(stmtIndent => PrintStatementList(body.Statements, stmtIndent))
                    : null)
            };

            PrintProperties(indent, properties);
        }

        private static void PrintStatementList(IReadOnlyList<StatementNode> statements, string indent)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                bool isLast = i == statements.Count - 1;
                var statement = statements[i];
                string header = $"{Ansi.Index($"[{i}]")} {Ansi.Node(statement.GetType().Name)}";
                PrintStatement(statement, indent, header, isLast);
            }
        }

        private static void PrintStatement(StatementNode stmt, string indent, string header, bool isLast)
        {
            PrintTreeLine(indent, isLast, header);
            string childIndent = ExtendIndent(indent, isLast);

            switch (stmt)
            {
                case AssignmentNode assign:
                    var assignmentProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabel("Target"), targetIndent => PrintExpression(assign.Target, targetIndent, true)),
                        (FormatLabel("Value"), valueIndent => PrintExpression(assign.Value, valueIndent, true))
                    };

                    PrintProperties(childIndent, assignmentProps);
                    break;

                case IfStatementNode ifStmt:
                    var ifProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabel("Condition"), conditionIndent => PrintExpression(ifStmt.Condition, conditionIndent, true)),
                        (FormatCount("ThenBody", ifStmt.ThenBody.Count, "statements"), ifStmt.ThenBody.Count > 0
                            ? new Action<string>(thenIndent => PrintStatementList(ifStmt.ThenBody, thenIndent))
                            : null)
                    };

                    if (ifStmt.ElseBody.Count > 0)
                    {
                        ifProps.Add((FormatCount("ElseBody", ifStmt.ElseBody.Count, "statements"), elseIndent => PrintStatementList(ifStmt.ElseBody, elseIndent)));
                    }

                    PrintProperties(childIndent, ifProps);
                    break;

                case WhileLoopNode whileLoop:
                    var whileProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabel("Condition"), conditionIndent => PrintExpression(whileLoop.Condition, conditionIndent, true)),
                        (FormatCount("Body", whileLoop.Body.Count, "statements"), whileLoop.Body.Count > 0
                            ? new Action<string>(bodyIndent => PrintStatementList(whileLoop.Body, bodyIndent))
                            : null)
                    };

                    PrintProperties(childIndent, whileProps);
                    break;

                case ForLoopNode forLoop:
                    string reverseText = forLoop.IsReverse ? "true" : "false";
                    var forProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabelValue("Variable", Ansi.Value($"\"{forLoop.Variable}\"")), null),
                        (FormatLabelValue("IsReverse", Ansi.Value(reverseText)), null),
                        (FormatLabel("Range"), rangeIndent => PrintExpression(forLoop.Range, rangeIndent, true)),
                        (FormatCount("Body", forLoop.Body.Count, "statements"), forLoop.Body.Count > 0
                            ? new Action<string>(bodyIndent => PrintStatementList(forLoop.Body, bodyIndent))
                            : null)
                    };

                    PrintProperties(childIndent, forProps);
                    break;

                case ReturnStatementNode retStmt:
                    var returnProps = new List<(string, Action<string>?)>();
                    if (retStmt.Value != null)
                    {
                        returnProps.Add((FormatLabel("Value"), valueIndent => PrintExpression(retStmt.Value, valueIndent, true)));
                    }
                    else
                    {
                        returnProps.Add((FormatLabelValue("Value", Ansi.Muted("(void return)")), null));
                    }

                    PrintProperties(childIndent, returnProps);
                    break;

                case PrintStatementNode printStmt:
                    var printProps = new List<(string, Action<string>?)>
                    {
                        (FormatLabel("Expression"), exprIndent => PrintExpression(printStmt.Expression, exprIndent, true))
                    };

                    PrintProperties(childIndent, printProps);
                    break;
            }
        }

        private static void PrintExpression(ExpressionNode expr, string indent, bool isLast)
        {
            if (expr == null)
            {
                PrintTreeLine(indent, isLast, Ansi.Muted("(null expression)"));
                return;
            }

            var properties = new List<(string, Action<string>?)>();

            switch (expr)
            {
                case IntegerLiteralNode intLit:
                    properties.Add((FormatLabelValue("Value", Ansi.Value(intLit.Value.ToString())), null));
                    break;

                case RealLiteralNode realLit:
                    properties.Add((FormatLabelValue("Value", Ansi.Value(realLit.Value.ToString())), null));
                    break;

                case BooleanLiteralNode boolLit:
                    string boolText = boolLit.Value ? "true" : "false";
                    properties.Add((FormatLabelValue("Value", Ansi.Value(boolText)), null));
                    break;

                case IdentifierNode id:
                    properties.Add((FormatLabelValue("Name", Ansi.Value($"\"{id.Name}\"")), null));
                    break;

                case BinaryOperationNode binOp:
                    properties.Add((FormatLabelValue("Operator", Ansi.Value($"\"{binOp.Operator}\"")), null));
                    properties.Add((FormatLabel("Left"), leftIndent => PrintExpression(binOp.Left, leftIndent, true)));
                    properties.Add((FormatLabel("Right"), rightIndent => PrintExpression(binOp.Right, rightIndent, true)));
                    break;

                case UnaryOperationNode unOp:
                    properties.Add((FormatLabelValue("Operator", Ansi.Value($"\"{unOp.Operator}\"")), null));
                    properties.Add((FormatLabel("Operand"), operandIndent => PrintExpression(unOp.Operand, operandIndent, true)));
                    break;

                case ArrayAccessNode arrAccess:
                    properties.Add((FormatLabel("Array"), arrayIndent => PrintExpression(arrAccess.Array, arrayIndent, true)));
                    properties.Add((FormatLabel("Index"), indexIndent => PrintExpression(arrAccess.Index, indexIndent, true)));
                    break;

                case RecordAccessNode recAccess:
                    properties.Add((FormatLabelValue("FieldName", Ansi.Value($"\"{recAccess.FieldName}\"")), null));
                    properties.Add((FormatLabel("Record"), recordIndent => PrintExpression(recAccess.Record, recordIndent, true)));
                    break;

                case RoutineCallNode call:
                    properties.Add((FormatLabelValue("RoutineName", Ansi.Value($"\"{call.RoutineName}\"")), null));
                    properties.Add((FormatCount("Arguments", call.Arguments.Count), call.Arguments.Count > 0
                        ? new Action<string>(argsIndent => PrintExpressionList(call.Arguments, argsIndent))
                        : null));
                    break;

                case RangeNode range:
                    properties.Add((FormatLabel("Start"), startIndent => PrintExpression(range.Start, startIndent, true)));
                    properties.Add((FormatLabel("End"), endIndent => PrintExpression(range.End, endIndent, true)));
                    break;

                case ArrayInitializerNode arrInit:
                    properties.Add((FormatCount("Elements", arrInit.Elements.Count), arrInit.Elements.Count > 0
                        ? new Action<string>(elementsIndent => PrintExpressionList(arrInit.Elements, elementsIndent))
                        : null));
                    break;
            }

            bool hasProperties = properties.Count > 0;
            PrintTreeLine(indent, isLast, Ansi.Node(expr.GetType().Name));

            if (hasProperties)
            {
                string nextIndent = ExtendIndent(indent, isLast);
                PrintProperties(nextIndent, properties);
            }
        }

        private static void PrintExpressionList(IReadOnlyList<ExpressionNode> expressions, string indent)
        {
            for (int i = 0; i < expressions.Count; i++)
            {
                bool isLast = i == expressions.Count - 1;
                PrintTreeLine(indent, isLast, Ansi.Index($"[{i}]"));
                string childIndent = ExtendIndent(indent, isLast);
                PrintExpression(expressions[i], childIndent, isLast);
            }
        }

        private static void PrintType(TypeNode type, string indent, bool isLast)
        {
            if (type == null)
            {
                PrintTreeLine(indent, isLast, Ansi.Muted("(null type)"));
                return;
            }

            var properties = new List<(string, Action<string>?)>();

            switch (type)
            {
                case PrimitiveTypeNode prim:
                    properties.Add((FormatLabelValue("TypeName", Ansi.Value($"\"{prim.TypeName}\"")), null));
                    break;

                case UserTypeNode user:
                    properties.Add((FormatLabelValue("TypeName", Ansi.Value($"\"{user.TypeName}\"")), null));
                    break;

                case ArrayTypeNode arr:
                    properties.Add((FormatLabel("Size"), sizeIndent => PrintExpression(arr.Size, sizeIndent, true)));
                    properties.Add((FormatLabel("ElementType"), typeIndent => PrintType(arr.ElementType, typeIndent, true)));
                    break;

                case RecordTypeNode rec:
                    properties.Add((FormatCount("Fields", rec.Fields.Count), rec.Fields.Count > 0
                        ? new Action<string>(fieldsIndent => PrintFieldList(rec.Fields, fieldsIndent))
                        : null));
                    break;
            }

            bool hasProperties = properties.Count > 0;
            PrintTreeLine(indent, isLast, Ansi.Node(type.GetType().Name));

            if (hasProperties)
            {
                string nextIndent = ExtendIndent(indent, isLast);
                PrintProperties(nextIndent, properties);
            }
        }

        private static void PrintFieldList(IReadOnlyList<FieldDeclarationNode> fields, string indent)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                bool isLast = i == fields.Count - 1;
                PrintTreeLine(indent, isLast, $"{Ansi.Index($"[{i}]")} {Ansi.Node(nameof(FieldDeclarationNode))}");
                string childIndent = ExtendIndent(indent, isLast);
                PrintFieldDeclaration(fields[i], childIndent);
            }
        }
    }
}

