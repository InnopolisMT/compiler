using Xunit;
using Compiler.Lexer;
using Compiler.Parser;
using Compiler.AST;
using System.Linq;

namespace Compiler.Tests
{
    public class ParserTests
    {
        // ============================================================================
        // HELPER METHODS
        // ============================================================================

        /// <summary>
        /// Parse code from string
        /// </summary>
        private ProgramNode ParseCode(string code)
        {
            var lexer = new LexerClass(code);
            var parser = new ParserFacade(lexer);
            return parser.Parse();
        }

        /// <summary>
        /// Parse code from file in parser_test_files directory
        /// </summary>
        private ProgramNode ParseFile(string fileName)
        {
            string filePath = Path.Combine("parser_test_files", fileName);
            string code = File.ReadAllText(filePath);
            return ParseCode(code);
        }

        /// <summary>
        /// Get single declaration of specific type from AST
        /// </summary>
        private T GetSingleDeclaration<T>(ProgramNode ast) where T : DeclarationNode
        {
            Assert.Single(ast.Declarations);
            return Assert.IsType<T>(ast.Declarations[0]);
        }

        /// <summary>
        /// Get declaration of specific type by index
        /// </summary>
        private T GetDeclaration<T>(ProgramNode ast, int index = 0) where T : DeclarationNode
        {
            var decls = ast.Declarations.OfType<T>().ToList();
            Assert.True(decls.Count > index, $"Expected at least {index + 1} declarations of type {typeof(T).Name}");
            return decls[index];
        }

        /// <summary>
        /// Get single statement from routine body
        /// </summary>
        private T GetSingleStatement<T>(RoutineDeclarationNode routine) where T : StatementNode
        {
            Assert.Single(routine.Body.Statements);
            return Assert.IsType<T>(routine.Body.Statements[0]);
        }

        // ============================================================================
        // DECLARATION TESTS
        // ============================================================================

        [Fact]
        public void TestSimpleVariableDeclaration()
        {
            var ast = ParseFile("simple_variable.txt");
            var varDecl = GetSingleDeclaration<VariableDeclarationNode>(ast);

            Assert.Equal("x", varDecl.Name);
            
            var type = Assert.IsType<PrimitiveTypeNode>(varDecl.Type);
            Assert.Equal("integer", type.TypeName);

            Assert.NotNull(varDecl.InitialValue);
            var initValue = Assert.IsType<IntegerLiteralNode>(varDecl.InitialValue);
            Assert.Equal(42L, initValue.Value);
        }

        [Fact]
        public void TestVariableWithoutInitialization()
        {
            var ast = ParseFile("variable_no_init.txt");
            var varDecl = GetSingleDeclaration<VariableDeclarationNode>(ast);

            Assert.Equal("x", varDecl.Name);
            Assert.IsType<PrimitiveTypeNode>(varDecl.Type);
            Assert.Null(varDecl.InitialValue);
        }

        [Fact]
        public void TestRecordTypeDeclaration()
        {
            var ast = ParseFile("record_type.txt");
            var typeDecl = GetSingleDeclaration<TypeDeclarationNode>(ast);

            Assert.Equal("Person", typeDecl.Name);
            
            var recordType = Assert.IsType<RecordTypeNode>(typeDecl.Type);
            Assert.Equal(2, recordType.Fields.Count);
            
            Assert.Equal("age", recordType.Fields[0].Name);
            Assert.IsType<PrimitiveTypeNode>(recordType.Fields[0].Type);
            
            Assert.Equal("name", recordType.Fields[1].Name);
            Assert.IsType<PrimitiveTypeNode>(recordType.Fields[1].Type);
        }

        [Fact]
        public void TestArrayTypeDeclaration()
        {
            var ast = ParseFile("array_type.txt");
            var typeDecl = GetSingleDeclaration<TypeDeclarationNode>(ast);

            Assert.Equal("IntArray", typeDecl.Name);
            
            var arrayType = Assert.IsType<ArrayTypeNode>(typeDecl.Type);
            
            var sizeExpr = Assert.IsType<IntegerLiteralNode>(arrayType.Size);
            Assert.Equal(5L, sizeExpr.Value);
            
            var elemType = Assert.IsType<PrimitiveTypeNode>(arrayType.ElementType);
            Assert.Equal("integer", elemType.TypeName);
        }

        [Fact]
        public void TestSimpleRoutineDeclaration()
        {
            var ast = ParseFile("simple_routine.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);

            Assert.Equal("main", routine.Name);
            Assert.Empty(routine.Parameters);
            Assert.Null(routine.ReturnType);
            Assert.Single(routine.Body.Statements);
        }

        [Fact]
        public void TestRoutineWithParameters()
        {
            var ast = ParseFile("routine_with_params.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);

            Assert.Equal("add", routine.Name);
            Assert.Equal(2, routine.Parameters.Count);
            
            // Check first parameter
            Assert.Equal("a", routine.Parameters[0].Name);
            var param1Type = Assert.IsType<PrimitiveTypeNode>(routine.Parameters[0].Type);
            Assert.Equal("integer", param1Type.TypeName);
            
            // Check second parameter
            Assert.Equal("b", routine.Parameters[1].Name);
            var param2Type = Assert.IsType<PrimitiveTypeNode>(routine.Parameters[1].Type);
            Assert.Equal("integer", param2Type.TypeName);

            // Check return type
            Assert.NotNull(routine.ReturnType);
            var returnType = Assert.IsType<PrimitiveTypeNode>(routine.ReturnType);
            Assert.Equal("integer", returnType.TypeName);

            // Check body has return statement
            Assert.Single(routine.Body.Statements);
            Assert.IsType<ReturnStatementNode>(routine.Body.Statements[0]);
        }

        // ============================================================================
        // STATEMENT TESTS
        // ============================================================================

        [Fact]
        public void TestSimpleAssignment()
        {
            var ast = ParseFile("assignment_simple.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var assignment = GetSingleStatement<AssignmentNode>(routine);

            var target = Assert.IsType<IdentifierNode>(assignment.Target);
            Assert.Equal("x", target.Name);

            var value = Assert.IsType<IntegerLiteralNode>(assignment.Value);
            Assert.Equal(42L, value.Value);
        }

        [Fact]
        public void TestRecordFieldAssignment()
        {
            var ast = ParseFile("assignment_record.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var assignment = GetSingleStatement<AssignmentNode>(routine);

            // Check target is RecordAccessNode
            var recordAccess = Assert.IsType<RecordAccessNode>(assignment.Target);
            Assert.Equal("age", recordAccess.FieldName);
            
            var record = Assert.IsType<IdentifierNode>(recordAccess.Record);
            Assert.Equal("person", record.Name);

            // Check value
            var value = Assert.IsType<IntegerLiteralNode>(assignment.Value);
            Assert.Equal(100L, value.Value);
        }

        [Fact]
        public void TestArrayElementAssignment()
        {
            var ast = ParseFile("assignment_array.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var assignment = GetSingleStatement<AssignmentNode>(routine);

            // Check target is ArrayAccessNode
            var arrayAccess = Assert.IsType<ArrayAccessNode>(assignment.Target);
            
            var array = Assert.IsType<IdentifierNode>(arrayAccess.Array);
            Assert.Equal("arr", array.Name);
            
            var index = Assert.IsType<IntegerLiteralNode>(arrayAccess.Index);
            Assert.Equal(5L, index.Value);

            // Check value
            var value = Assert.IsType<IntegerLiteralNode>(assignment.Value);
            Assert.Equal(42L, value.Value);
        }

        [Fact]
        public void TestIfThenStatement()
        {
            var ast = ParseFile("if_then.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var ifStmt = GetSingleStatement<IfStatementNode>(routine);

            // Check condition
            var condition = Assert.IsType<BinaryOperationNode>(ifStmt.Condition);
            Assert.Equal("=", condition.Operator);

            // Check then body
            Assert.Single(ifStmt.ThenBody);
            Assert.IsType<PrintStatementNode>(ifStmt.ThenBody[0]);

            // Check else is empty
            Assert.Empty(ifStmt.ElseBody);
        }

        [Fact]
        public void TestIfThenElseStatement()
        {
            var ast = ParseFile("if_then_else.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var ifStmt = GetSingleStatement<IfStatementNode>(routine);

            // Check condition
            var condition = Assert.IsType<BinaryOperationNode>(ifStmt.Condition);
            Assert.Equal(">", condition.Operator);

            // Check then body
            Assert.Single(ifStmt.ThenBody);
            Assert.IsType<PrintStatementNode>(ifStmt.ThenBody[0]);

            // Check else body exists
            Assert.Single(ifStmt.ElseBody);
            Assert.IsType<PrintStatementNode>(ifStmt.ElseBody[0]);
        }

        [Fact]
        public void TestForLoop()
        {
            var ast = ParseFile("for_loop.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var forLoop = GetSingleStatement<ForLoopNode>(routine);

            Assert.Equal("i", forLoop.Variable);
            Assert.False(forLoop.IsReverse);

            // Check range
            var range = Assert.IsType<RangeNode>(forLoop.Range);
            var rangeStart = Assert.IsType<IntegerLiteralNode>(range.Start);
            Assert.Equal(1L, rangeStart.Value);
            
            var rangeEnd = Assert.IsType<IntegerLiteralNode>(range.End);
            Assert.Equal(10L, rangeEnd.Value);

            // Check body
            Assert.Single(forLoop.Body);
            Assert.IsType<PrintStatementNode>(forLoop.Body[0]);
        }

        [Fact]
        public void TestReverseForLoop()
        {
            var ast = ParseFile("for_loop_reverse.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var forLoop = GetSingleStatement<ForLoopNode>(routine);

            Assert.Equal("i", forLoop.Variable);
            Assert.True(forLoop.IsReverse); // Key difference!

            var range = Assert.IsType<RangeNode>(forLoop.Range);
            Assert.Equal(10L, ((IntegerLiteralNode)range.Start).Value);
            Assert.Equal(1L, ((IntegerLiteralNode)range.End).Value);
        }

        [Fact]
        public void TestWhileLoop()
        {
            var ast = ParseFile("while_loop.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var whileLoop = GetSingleStatement<WhileLoopNode>(routine);

            // Check condition
            var condition = Assert.IsType<BinaryOperationNode>(whileLoop.Condition);
            Assert.Equal("<", condition.Operator);

            // Check body
            Assert.Single(whileLoop.Body);
            Assert.IsType<PrintStatementNode>(whileLoop.Body[0]);
        }

        // ============================================================================
        // EXPRESSION TESTS
        // ============================================================================

        [Fact]
        public void TestBinaryExpressionPrecedence()
        {
            var ast = ParseFile("binary_expression.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var assignment = Assert.IsType<AssignmentNode>(routine.Body.Statements[0]);

            // Check that x := 10 + 20 * 3 is parsed as 10 + (20 * 3)
            var plus = Assert.IsType<BinaryOperationNode>(assignment.Value);
            Assert.Equal("+", plus.Operator);

            var left = Assert.IsType<IntegerLiteralNode>(plus.Left);
            Assert.Equal(10L, left.Value);

            var multiply = Assert.IsType<BinaryOperationNode>(plus.Right);
            Assert.Equal("*", multiply.Operator);
            Assert.Equal(20L, ((IntegerLiteralNode)multiply.Left).Value);
            Assert.Equal(3L, ((IntegerLiteralNode)multiply.Right).Value);
        }

        [Fact]
        public void TestOperatorPrecedence()
        {
            var ast = ParseFile("operator_precedence.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            
            Assert.Equal(3, routine.Body.Statements.Count);

            // First: x := 2 + 3 * 4 should be 2 + (3 * 4)
            var assign1 = Assert.IsType<AssignmentNode>(routine.Body.Statements[0]);
            var expr1 = Assert.IsType<BinaryOperationNode>(assign1.Value);
            Assert.Equal("+", expr1.Operator);
            Assert.IsType<BinaryOperationNode>(expr1.Right); // should be multiplication

            // Second: y := 10 - 2 / 2 should be 10 - (2 / 2)
            var assign2 = Assert.IsType<AssignmentNode>(routine.Body.Statements[1]);
            var expr2 = Assert.IsType<BinaryOperationNode>(assign2.Value);
            Assert.Equal("-", expr2.Operator);
            Assert.IsType<BinaryOperationNode>(expr2.Right); // should be division

            // Third: z := (5 + 3) * 2 - parentheses override precedence
            var assign3 = Assert.IsType<AssignmentNode>(routine.Body.Statements[2]);
            var expr3 = Assert.IsType<BinaryOperationNode>(assign3.Value);
            Assert.Equal("*", expr3.Operator);
            var leftParen = Assert.IsType<BinaryOperationNode>(expr3.Left);
            Assert.Equal("+", leftParen.Operator);
        }

        [Fact]
        public void TestLogicalExpression()
        {
            var ast = ParseFile("logical_expression.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var ifStmt = GetSingleStatement<IfStatementNode>(routine);

            // Check that condition is "and" operation
            var andOp = Assert.IsType<BinaryOperationNode>(ifStmt.Condition);
            Assert.Equal("and", andOp.Operator);

            // Left side: x > 5
            var leftCmp = Assert.IsType<BinaryOperationNode>(andOp.Left);
            Assert.Equal(">", leftCmp.Operator);

            // Right side: y < 10
            var rightCmp = Assert.IsType<BinaryOperationNode>(andOp.Right);
            Assert.Equal("<", rightCmp.Operator);
        }

        [Fact]
        public void TestUnaryOperations()
        {
            var ast = ParseFile("unary_operations.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            
            Assert.Equal(2, routine.Body.Statements.Count);

            // First: x := -42
            var assignment = Assert.IsType<AssignmentNode>(routine.Body.Statements[0]);
            var unaryMinus = Assert.IsType<UnaryOperationNode>(assignment.Value);
            Assert.Equal("-", unaryMinus.Operator);
            Assert.Equal(42L, ((IntegerLiteralNode)unaryMinus.Operand).Value);

            // Second: if (not flag) then...
            var ifStmt = Assert.IsType<IfStatementNode>(routine.Body.Statements[1]);
            var notOp = Assert.IsType<UnaryOperationNode>(ifStmt.Condition);
            Assert.Equal("not", notOp.Operator);
            Assert.IsType<IdentifierNode>(notOp.Operand);
        }

        [Fact]
        public void TestArrayInitializer()
        {
            var ast = ParseFile("array_initializer.txt");
            var varDecl = GetSingleDeclaration<VariableDeclarationNode>(ast);

            Assert.Equal("arr", varDecl.Name);
            
            var arrayInit = Assert.IsType<ArrayInitializerNode>(varDecl.InitialValue);
            Assert.Equal(5, arrayInit.Elements.Count);

            // Check all elements are integers
            for (int i = 0; i < 5; i++)
            {
                var element = Assert.IsType<IntegerLiteralNode>(arrayInit.Elements[i]);
                Assert.Equal(i + 1L, element.Value);
            }
        }

        [Fact]
        public void TestNestedRecordAccess()
        {
            var ast = ParseFile("nested_record_access.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            
            Assert.Equal(2, routine.Body.Statements.Count);

            // First: person.addr.street := 100
            var assignment = Assert.IsType<AssignmentNode>(routine.Body.Statements[0]);
            
            // Should be RecordAccessNode with nested structure
            // person.addr.street should be: RecordAccess("street", RecordAccess("addr", Identifier("person")))
            var outerAccess = Assert.IsType<RecordAccessNode>(assignment.Target);
            Assert.Equal("street", outerAccess.FieldName);
            
            var middleAccess = Assert.IsType<RecordAccessNode>(outerAccess.Record);
            Assert.Equal("addr", middleAccess.FieldName);
            
            var innerIdent = Assert.IsType<IdentifierNode>(middleAccess.Record);
            Assert.Equal("person", innerIdent.Name);

            // Second: print person.addr.street
            var printStmt = Assert.IsType<PrintStatementNode>(routine.Body.Statements[1]);
            
            var printAccess = Assert.IsType<RecordAccessNode>(printStmt.Expression);
            Assert.Equal("street", printAccess.FieldName);
            
            var printMiddle = Assert.IsType<RecordAccessNode>(printAccess.Record);
            Assert.Equal("addr", printMiddle.FieldName);
            
            var printIdent = Assert.IsType<IdentifierNode>(printMiddle.Record);
            Assert.Equal("person", printIdent.Name);
        }

        [Fact]
        public void TestNestedRecordDeclaration()
        {
            var ast = ParseFile("nested_record_declaration.txt");
            
            Assert.Equal(3, ast.Declarations.Count);
            var types = ast.Declarations.OfType<TypeDeclarationNode>().ToList();
            Assert.Equal(3, types.Count);

            // Check Address type
            var addressType = types[0];
            Assert.Equal("Address", addressType.Name);
            var addressRecord = Assert.IsType<RecordTypeNode>(addressType.Type);
            Assert.Equal(2, addressRecord.Fields.Count);

            // Check Person type - has Address field
            var personType = types[1];
            Assert.Equal("Person", personType.Name);
            var personRecord = Assert.IsType<RecordTypeNode>(personType.Type);
            Assert.Equal(2, personRecord.Fields.Count);
            
            var addrField = personRecord.Fields[1];
            Assert.Equal("addr", addrField.Name);
            var addrFieldType = Assert.IsType<UserTypeNode>(addrField.Type);
            Assert.Equal("Address", addrFieldType.TypeName);

            // Check Company type - has Person field
            var companyType = types[2];
            Assert.Equal("Company", companyType.Name);
            var companyRecord = Assert.IsType<RecordTypeNode>(companyType.Type);
            Assert.Equal(2, companyRecord.Fields.Count);
            
            var ownerField = companyRecord.Fields[0];
            Assert.Equal("owner", ownerField.Name);
            var ownerFieldType = Assert.IsType<UserTypeNode>(ownerField.Type);
            Assert.Equal("Person", ownerFieldType.TypeName);
        }

        [Fact]
        public void TestNestedRecordUsageInExpressions()
        {
            var ast = ParseFile("nested_record_usage.txt");
            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            
            Assert.Equal(3, routine.Body.Statements.Count);

            // First: company.owner.addr.street := 123 (4 levels!)
            var assignment = Assert.IsType<AssignmentNode>(routine.Body.Statements[0]);
            
            // Verify 4-level nesting: company.owner.addr.street
            var level4 = Assert.IsType<RecordAccessNode>(assignment.Target);
            Assert.Equal("street", level4.FieldName);
            
            var level3 = Assert.IsType<RecordAccessNode>(level4.Record);
            Assert.Equal("addr", level3.FieldName);
            
            var level2 = Assert.IsType<RecordAccessNode>(level3.Record);
            Assert.Equal("owner", level2.FieldName);
            
            var level1 = Assert.IsType<IdentifierNode>(level2.Record);
            Assert.Equal("company", level1.Name);

            // Second: if condition with nested record access
            var ifStmt = Assert.IsType<IfStatementNode>(routine.Body.Statements[1]);
            var condition = Assert.IsType<BinaryOperationNode>(ifStmt.Condition);
            
            // Left side of comparison should be company.owner.addr.number
            var conditionAccess = Assert.IsType<RecordAccessNode>(condition.Left);
            Assert.Equal("number", conditionAccess.FieldName);
            // (не проверяем всю цепочку повторно, главное что это RecordAccess)

            // Third: assignment with nested record in expression
            var assignWithExpr = Assert.IsType<AssignmentNode>(routine.Body.Statements[2]);
            var binaryOp = Assert.IsType<BinaryOperationNode>(assignWithExpr.Value);
            Assert.Equal("+", binaryOp.Operator);
            
            // Left side of + should be nested record access
            var exprAccess = Assert.IsType<RecordAccessNode>(binaryOp.Left);
            Assert.Equal("street", exprAccess.FieldName);
        }

        [Fact]
        public void TestRoutineCall()
        {
            var ast = ParseFile("routine_call.txt");
            
            Assert.Equal(2, ast.Declarations.Count);
            var routines = ast.Declarations.OfType<RoutineDeclarationNode>().ToList();
            Assert.Equal(2, routines.Count);

            var fooRoutine = routines[0];
            Assert.Equal("foo", fooRoutine.Name);
            Assert.Single(fooRoutine.Body.Statements);

            var mainRoutine = routines[1];
            Assert.Equal("main", mainRoutine.Name);
            Assert.Single(mainRoutine.Body.Statements);
        }

        // ============================================================================
        // COMPLEX PROGRAM TESTS
        // ============================================================================

        [Fact]
        public void TestMultipleDeclarations()
        {
            var ast = ParseFile("multiple_declarations.txt");

            Assert.Equal(5, ast.Declarations.Count); // 3 vars + 1 type + 1 routine

            // Check variables
            var variables = ast.Declarations.OfType<VariableDeclarationNode>().ToList();
            Assert.Equal(3, variables.Count);
            Assert.Equal("x", variables[0].Name);
            Assert.Equal("y", variables[1].Name);
            Assert.Equal("flag", variables[2].Name);

            // Check type
            var types = ast.Declarations.OfType<TypeDeclarationNode>().ToList();
            Assert.Single(types);
            Assert.Equal("Point", types[0].Name);

            // Check routine
            var routines = ast.Declarations.OfType<RoutineDeclarationNode>().ToList();
            Assert.Single(routines);
            Assert.Equal("main", routines[0].Name);
        }

        [Fact]
        public void TestCompleteProgram()
        {
            var ast = ParseFile("complete_program.txt");

            // Should have 4 declarations: 3 types + 1 global var + 1 routine
            Assert.Equal(5, ast.Declarations.Count);

            // Check type declarations
            var types = ast.Declarations.OfType<TypeDeclarationNode>().ToList();
            Assert.Equal(3, types.Count);
            Assert.Equal("Address", types[0].Name);
            Assert.Equal("Person", types[1].Name);
            Assert.Equal("IntArray", types[2].Name);

            // Check global variable
            var globals = ast.Declarations.OfType<VariableDeclarationNode>().ToList();
            Assert.Single(globals);
            Assert.Equal("globalVar", globals[0].Name);

            // Check routine
            var routines = ast.Declarations.OfType<RoutineDeclarationNode>().ToList();
            Assert.Single(routines);
            
            var mainRoutine = routines[0];
            Assert.Equal("main", mainRoutine.Name);
            
            // Check routine body has local declarations
            Assert.Equal(2, mainRoutine.Body.Declarations.Count);
            
            // Check routine body has statements
            Assert.Equal(3, mainRoutine.Body.Statements.Count);
            
            // Verify statement types
            Assert.IsType<AssignmentNode>(mainRoutine.Body.Statements[0]);
            Assert.IsType<ForLoopNode>(mainRoutine.Body.Statements[1]);
            Assert.IsType<IfStatementNode>(mainRoutine.Body.Statements[2]);
        }

        // ============================================================================
        // ERROR HANDLING TESTS
        // ============================================================================

        [Fact]
        public void TestMissingEnd_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => ParseFile("error_missing_end.txt"));
        }

        [Fact]
        public void TestInvalidSyntax_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => ParseFile("error_invalid_syntax.txt"));
        }

        [Fact]
        public void TestUnexpectedToken_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => ParseFile("error_unexpected_token.txt"));
        }

        // ============================================================================
        // EXPRESSION STRUCTURE TESTS
        // ============================================================================

        [Fact]
        public void TestRecordAccessInExpression()
        {
            var ast = ParseCode(@"
                routine main()
                    for i in person.age .. 50 loop
                        print i
                    end
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var forLoop = GetSingleStatement<ForLoopNode>(routine);

            var range = Assert.IsType<RangeNode>(forLoop.Range);
            
            // Start should be person.age (RecordAccessNode)
            var recordAccess = Assert.IsType<RecordAccessNode>(range.Start);
            Assert.Equal("age", recordAccess.FieldName);
            Assert.Equal("person", ((IdentifierNode)recordAccess.Record).Name);

            // End should be 50
            var endValue = Assert.IsType<IntegerLiteralNode>(range.End);
            Assert.Equal(50L, endValue.Value);
        }

        [Fact]
        public void TestArrayAccessInExpression()
        {
            var ast = ParseCode(@"
                routine main()
                    print arr[5]
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var printStmt = GetSingleStatement<PrintStatementNode>(routine);

            var arrayAccess = Assert.IsType<ArrayAccessNode>(printStmt.Expression);
            Assert.Equal("arr", ((IdentifierNode)arrayAccess.Array).Name);
            Assert.Equal(5L, ((IntegerLiteralNode)arrayAccess.Index).Value);
        }

        [Fact]
        public void TestReturnStatement()
        {
            var ast = ParseCode(@"
                routine test() : integer
                    return 42
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var returnStmt = GetSingleStatement<ReturnStatementNode>(routine);

            Assert.NotNull(returnStmt.Value);
            var returnValue = Assert.IsType<IntegerLiteralNode>(returnStmt.Value);
            Assert.Equal(42L, returnValue.Value);
        }

        [Fact]
        public void TestReturnVoid()
        {
            var ast = ParseCode(@"
                routine test()
                    return
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var returnStmt = GetSingleStatement<ReturnStatementNode>(routine);

            Assert.Null(returnStmt.Value);
        }

        [Fact]
        public void TestPrintStatement()
        {
            var ast = ParseCode(@"
                routine main()
                    print 42
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var printStmt = GetSingleStatement<PrintStatementNode>(routine);

            var value = Assert.IsType<IntegerLiteralNode>(printStmt.Expression);
            Assert.Equal(42L, value.Value);
        }

        // ============================================================================
        // TYPE TESTS
        // ============================================================================

        [Fact]
        public void TestPrimitiveTypes()
        {
            var ast = ParseCode(@"
                var i : integer is 0
                var r : real is 0.0
                var b : boolean is true");

            Assert.Equal(3, ast.Declarations.Count);

            var intVar = Assert.IsType<VariableDeclarationNode>(ast.Declarations[0]);
            var intType = Assert.IsType<PrimitiveTypeNode>(intVar.Type);
            Assert.Equal("integer", intType.TypeName);

            var realVar = Assert.IsType<VariableDeclarationNode>(ast.Declarations[1]);
            var realType = Assert.IsType<PrimitiveTypeNode>(realVar.Type);
            Assert.Equal("real", realType.TypeName);

            var boolVar = Assert.IsType<VariableDeclarationNode>(ast.Declarations[2]);
            var boolType = Assert.IsType<PrimitiveTypeNode>(boolVar.Type);
            Assert.Equal("boolean", boolType.TypeName);
        }

        [Fact]
        public void TestUserDefinedType()
        {
            var ast = ParseCode(@"
                type MyType is integer
                var x : MyType is 0");

            Assert.Equal(2, ast.Declarations.Count);

            var varDecl = GetDeclaration<VariableDeclarationNode>(ast, 0);
            var userType = Assert.IsType<UserTypeNode>(varDecl.Type);
            Assert.Equal("MyType", userType.TypeName);
        }

        // ============================================================================
        // EDGE CASES
        // ============================================================================

        [Fact]
        public void TestEmptyProgram()
        {
            var ast = ParseCode("");
            Assert.Empty(ast.Declarations);
        }

        [Fact]
        public void TestEmptyRoutineBody()
        {
            var ast = ParseCode(@"
                routine empty()
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            Assert.Empty(routine.Body.Declarations);
            Assert.Empty(routine.Body.Statements);
        }

        [Fact]
        public void TestEmptyRecordType()
        {
            var ast = ParseCode(@"
                type Empty is record
                end");

            var typeDecl = GetSingleDeclaration<TypeDeclarationNode>(ast);
            var recordType = Assert.IsType<RecordTypeNode>(typeDecl.Type);
            Assert.Empty(recordType.Fields);
        }

        [Fact]
        public void TestEmptyArrayInitializer()
        {
            var ast = ParseCode(@"
                var arr : IntArray is []");

            var varDecl = GetSingleDeclaration<VariableDeclarationNode>(ast);
            var arrayInit = Assert.IsType<ArrayInitializerNode>(varDecl.InitialValue);
            Assert.Empty(arrayInit.Elements);
        }

        // ============================================================================
        // INTEGRATION TESTS - FULL PROGRAMS
        // ============================================================================

        [Fact]
        public void TestCompleteProgram_CorrectStructure()
        {
            var ast = ParseFile("complete_program.txt");

            // Verify overall structure
            Assert.True(ast.Declarations.Count >= 4, "Should have at least 4 top-level declarations");

            // Verify has types
            var types = ast.Declarations.OfType<TypeDeclarationNode>().ToList();
            Assert.True(types.Count >= 3, "Should have at least 3 type declarations");

            // Verify has routine
            var routines = ast.Declarations.OfType<RoutineDeclarationNode>().ToList();
            Assert.True(routines.Count >= 1, "Should have at least 1 routine");

            var mainRoutine = routines.First(r => r.Name == "main");
            Assert.NotNull(mainRoutine);
            
            // Verify routine has body
            Assert.True(mainRoutine.Body.Statements.Count > 0, "Main routine should have statements");
        }

        [Fact]
        public void TestNestedControlStructures()
        {
            var ast = ParseCode(@"
                routine main()
                    for i in 1 .. 10 loop
                        if (i = 5) then
                            print i
                        end
                    end
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var forLoop = GetSingleStatement<ForLoopNode>(routine);

            // Check nested if inside for loop
            Assert.Single(forLoop.Body);
            var ifStmt = Assert.IsType<IfStatementNode>(forLoop.Body[0]);
            
            Assert.Single(ifStmt.ThenBody);
            Assert.IsType<PrintStatementNode>(ifStmt.ThenBody[0]);
        }

        [Fact]
        public void TestComplexExpression()
        {
            var ast = ParseCode(@"
                routine main()
                    result := (a + b) * c - d / 2
                end");

            var routine = GetSingleDeclaration<RoutineDeclarationNode>(ast);
            var assignment = GetSingleStatement<AssignmentNode>(routine);

            // Should parse as: ((a + b) * c) - (d / 2)
            var minus = Assert.IsType<BinaryOperationNode>(assignment.Value);
            Assert.Equal("-", minus.Operator);

            // Left side: (a + b) * c
            var multiply = Assert.IsType<BinaryOperationNode>(minus.Left);
            Assert.Equal("*", multiply.Operator);

            // Right side: d / 2
            var divide = Assert.IsType<BinaryOperationNode>(minus.Right);
            Assert.Equal("/", divide.Operator);
        }
    }
}

