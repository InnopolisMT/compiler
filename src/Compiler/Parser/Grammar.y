%namespace Compiler.Parser
%using Compiler.AST;
%using Compiler.Lexer;
%using System.Collections.Generic;

%output=Generated/Parser.cs

%YYSTYPE object
%YYLTYPE Compiler.Parser.LexLocation

// Token definitions from our lexer
%token tkIntegerLiteral tkRealLiteral tkBoolLiteral tkIdentifier
%token tkVar tkRealKeyword tkBoolKeyword tkIntegerKeyword tkType tkIs
%token tkIf tkThen tkElse tkWhile tkFor tkIn tkRange tkLoop tkReverse
%token tkRecord tkRecordAccess tkRoutine tkReturn tkArray tkEnd tkPrint
%token tkPlus tkMinus tkMultiply tkDivide tkMod tkAssign
%token tkLessThan tkLessThanOrEqual tkGreaterThan tkGreaterThanOrEqual
%token tkEqual tkNotEqual tkAnd tkOr tkNot tkXor
%token tkDot tkColon tkComma tkLeftParen tkRightParen tkLeftBracket tkRightBracket
%token tkInvalid

// Operator precedence (from low to high)
%left tkOr tkXor
%left tkAnd
%left tkEqual tkNotEqual
%left tkLessThan tkLessThanOrEqual tkGreaterThan tkGreaterThanOrEqual
%left tkPlus tkMinus
%left tkMultiply tkDivide tkMod
%right tkNot UNARY_MINUS
%left tkDot tkLeftBracket

// Start symbol
%start Program

%%

// ============================================================================
// Program
// ============================================================================

Program
    : DeclarationList
        {
            Result = new ProgramNode { Declarations = (List<DeclarationNode>)$1 };
            $$ = Result;
        }
    | /* empty */
        {
            Result = new ProgramNode();
            $$ = Result;
        }
    ;

DeclarationList
    : Declaration
        {
            $$ = new List<DeclarationNode> { (DeclarationNode)$1 };
        }
    | DeclarationList Declaration
        {
            var list = (List<DeclarationNode>)$1;
            list.Add((DeclarationNode)$2);
            $$ = list;
        }
    ;

Declaration
    : VariableDeclaration
    | TypeDeclaration
    | RoutineDeclaration
    ;

// ============================================================================
// Variable declaration: var x : integer is 42
// ============================================================================

VariableDeclaration
    : tkVar tkIdentifier tkColon Type tkIs Expression
        {
            $$ = new VariableDeclarationNode
            {
                Name = (string)$2,
                Type = (TypeNode)$4,
                InitialValue = (ExpressionNode)$6
            };
        }
    | tkVar tkIdentifier tkColon Type
        {
            $$ = new VariableDeclarationNode
            {
                Name = (string)$2,
                Type = (TypeNode)$4
            };
        }
    ;

// ============================================================================
// Type declaration: type IntArray is array[5] integer
// ============================================================================

TypeDeclaration
    : tkType tkIdentifier tkIs Type
        {
            $$ = new TypeDeclarationNode
            {
                Name = (string)$2,
                Type = (TypeNode)$4
            };
        }
    ;

Type
    : PrimitiveType
    | tkIdentifier
        {
            $$ = new UserTypeNode { TypeName = (string)$1 };
        }
    | ArrayType
    | RecordType
    ;

PrimitiveType
    : tkIntegerKeyword
        {
            $$ = new PrimitiveTypeNode { TypeName = "integer" };
        }
    | tkRealKeyword
        {
            $$ = new PrimitiveTypeNode { TypeName = "real" };
        }
    | tkBoolKeyword
        {
            $$ = new PrimitiveTypeNode { TypeName = "boolean" };
        }
    ;

ArrayType
    : tkArray tkLeftBracket Expression tkRightBracket Type
        {
            $$ = new ArrayTypeNode
            {
                Size = (ExpressionNode)$3,
                ElementType = (TypeNode)$5
            };
        }
    ;

RecordType
    : tkRecord VariableDeclarationList tkEnd
        {
            $$ = new RecordTypeNode { Fields = (List<VariableDeclarationNode>)$2 };
        }
    | tkRecord tkEnd
        {
            $$ = new RecordTypeNode();
        }
    ;

VariableDeclarationList
    : VariableDeclaration
        {
            $$ = new List<VariableDeclarationNode> { (VariableDeclarationNode)$1 };
        }
    | VariableDeclarationList VariableDeclaration
        {
            var list = (List<VariableDeclarationNode>)$1;
            list.Add((VariableDeclarationNode)$2);
            $$ = list;
        }
    ;

// ============================================================================
// Routine declaration: routine main() ... end
// ============================================================================

RoutineDeclaration
    : tkRoutine tkIdentifier tkLeftParen ParameterList tkRightParen tkColon Type Body tkEnd
        {
            $$ = new RoutineDeclarationNode
            {
                Name = (string)$2,
                Parameters = (List<ParameterNode>)$4,
                ReturnType = (TypeNode)$7,
                Body = (BodyNode)$8
            };
        }
    | tkRoutine tkIdentifier tkLeftParen ParameterList tkRightParen Body tkEnd
        {
            $$ = new RoutineDeclarationNode
            {
                Name = (string)$2,
                Parameters = (List<ParameterNode>)$4,
                Body = (BodyNode)$6
            };
        }
    | tkRoutine tkIdentifier tkLeftParen tkRightParen tkColon Type Body tkEnd
        {
            $$ = new RoutineDeclarationNode
            {
                Name = (string)$2,
                ReturnType = (TypeNode)$6,
                Body = (BodyNode)$7
            };
        }
    | tkRoutine tkIdentifier tkLeftParen tkRightParen Body tkEnd
        {
            $$ = new RoutineDeclarationNode
            {
                Name = (string)$2,
                Body = (BodyNode)$5
            };
        }
    ;

ParameterList
    : Parameter
        {
            $$ = new List<ParameterNode> { (ParameterNode)$1 };
        }
    | ParameterList tkComma Parameter
        {
            var list = (List<ParameterNode>)$1;
            list.Add((ParameterNode)$3);
            $$ = list;
        }
    ;

Parameter
    : tkIdentifier tkColon Type
        {
            $$ = new ParameterNode
            {
                Name = (string)$1,
                Type = (TypeNode)$3
            };
        }
    ;

Body
    : SimpleBody
    | DeclarationList SimpleBody
        {
            var body = (BodyNode)$2;
            body.Declarations = (List<DeclarationNode>)$1;
            $$ = body;
        }
    ;

SimpleBody
    : StatementList
        {
            $$ = new BodyNode { Statements = (List<StatementNode>)$1 };
        }
    | /* empty */
        {
            $$ = new BodyNode();
        }
    ;

// ============================================================================
// Statements
// ============================================================================

StatementList
    : Statement
        {
            $$ = new List<StatementNode> { (StatementNode)$1 };
        }
    | StatementList Statement
        {
            var list = (List<StatementNode>)$1;
            list.Add((StatementNode)$2);
            $$ = list;
        }
    ;

Statement
    : Assignment
    | IfStatement
    | WhileLoop
    | ForLoop
    | ReturnStatement
    | PrintStatement
    ;

Assignment
    : ModifiablePrimary tkAssign Expression
        {
            $$ = new AssignmentNode
            {
                Target = (ExpressionNode)$1,
                Value = (ExpressionNode)$3
            };
        }
    ;

IfStatement
    : tkIf tkLeftParen Expression tkRightParen tkThen Body tkEnd
        {
            $$ = new IfStatementNode
            {
                Condition = (ExpressionNode)$3,
                ThenBody = ((BodyNode)$6).Statements
            };
        }
    | tkIf tkLeftParen Expression tkRightParen tkThen Body tkElse Body tkEnd
        {
            $$ = new IfStatementNode
            {
                Condition = (ExpressionNode)$3,
                ThenBody = ((BodyNode)$6).Statements,
                ElseBody = ((BodyNode)$8).Statements
            };
        }
    ;

WhileLoop
    : tkWhile tkLeftParen Expression tkRightParen tkLoop Body tkEnd
        {
            $$ = new WhileLoopNode
            {
                Condition = (ExpressionNode)$3,
                Body = ((BodyNode)$6).Statements
            };
        }
    ;

ForLoop
    : tkFor tkIdentifier tkIn Range tkLoop Body tkEnd
        {
            $$ = new ForLoopNode
            {
                Variable = (string)$2,
                Range = (ExpressionNode)$4,
                Body = ((BodyNode)$6).Statements
            };
        }
    | tkFor tkIdentifier tkIn tkReverse Range tkLoop Body tkEnd
        {
            $$ = new ForLoopNode
            {
                Variable = (string)$2,
                Range = (ExpressionNode)$5,
                IsReverse = true,
                Body = ((BodyNode)$7).Statements
            };
        }
    ;

Range
    : Expression tkRange Expression
        {
            $$ = new RangeNode
            {
                Start = (ExpressionNode)$1,
                End = (ExpressionNode)$3
            };
        }
    ;

ReturnStatement
    : tkReturn Expression
        {
            $$ = new ReturnStatementNode { Value = (ExpressionNode)$2 };
        }
    | tkReturn
        {
            $$ = new ReturnStatementNode();
        }
    ;

PrintStatement
    : tkPrint Expression
        {
            $$ = new PrintStatementNode { Expression = (ExpressionNode)$2 };
        }
    ;

// ============================================================================
// Expressions
// ============================================================================

Expression
    : Primary
    | BinaryOperation
    | UnaryOperation
    ;

Primary
    : tkIntegerLiteral
        {
            $$ = new IntegerLiteralNode { Value = (long)$1 };
        }
    | tkRealLiteral
        {
            $$ = new RealLiteralNode { Value = (double)$1 };
        }
    | tkBoolLiteral
        {
            $$ = new BooleanLiteralNode { Value = (bool)$1 };
        }
    | ModifiablePrimary
    | tkLeftParen Expression tkRightParen
        {
            $$ = (ExpressionNode)$2;
        }
    | ArrayInitializer
    ;

ModifiablePrimary
    : tkIdentifier
        {
            $$ = new IdentifierNode { Name = (string)$1 };
        }
    | tkIdentifier tkLeftBracket Expression tkRightBracket
        {
            $$ = new ArrayAccessNode
            {
                Array = new IdentifierNode { Name = (string)$1 },
                Index = (ExpressionNode)$3
            };
        }
    | tkRecordAccess
        {
            // Handle composite record access (e.g., person.addr.street)
            var parts = ((string)$1).Split('.');
            ExpressionNode current = new IdentifierNode { Name = parts[0] };
            
            for (int i = 1; i < parts.Length; i++)
            {
                current = new RecordAccessNode
                {
                    Record = current,
                    FieldName = parts[i]
                };
            }
            
            $$ = current;
        }
    | ModifiablePrimary tkDot tkIdentifier
        {
            $$ = new RecordAccessNode
            {
                Record = (ExpressionNode)$1,
                FieldName = (string)$3
            };
        }
    | RoutineCall
    ;

RoutineCall
    : tkIdentifier tkLeftParen ArgumentList tkRightParen
        {
            $$ = new RoutineCallNode
            {
                RoutineName = (string)$1,
                Arguments = (List<ExpressionNode>)$3
            };
        }
    | tkIdentifier tkLeftParen tkRightParen
        {
            $$ = new RoutineCallNode
            {
                RoutineName = (string)$1
            };
        }
    ;

ArgumentList
    : Expression
        {
            $$ = new List<ExpressionNode> { (ExpressionNode)$1 };
        }
    | ArgumentList tkComma Expression
        {
            var list = (List<ExpressionNode>)$1;
            list.Add((ExpressionNode)$3);
            $$ = list;
        }
    ;

ArrayInitializer
    : tkLeftBracket ExpressionList tkRightBracket
        {
            $$ = new ArrayInitializerNode { Elements = (List<ExpressionNode>)$2 };
        }
    | tkLeftBracket tkRightBracket
        {
            $$ = new ArrayInitializerNode();
        }
    ;

ExpressionList
    : Expression
        {
            $$ = new List<ExpressionNode> { (ExpressionNode)$1 };
        }
    | ExpressionList tkComma Expression
        {
            var list = (List<ExpressionNode>)$1;
            list.Add((ExpressionNode)$3);
            $$ = list;
        }
    ;

BinaryOperation
    : Expression tkPlus Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "+",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkMinus Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "-",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkMultiply Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "*",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkDivide Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "/",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkMod Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "%",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkEqual Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "=",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkNotEqual Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "/=",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkLessThan Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "<",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkLessThanOrEqual Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "<=",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkGreaterThan Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = ">",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkGreaterThanOrEqual Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = ">=",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkAnd Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "and",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkOr Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "or",
                Right = (ExpressionNode)$3
            };
        }
    | Expression tkXor Expression
        {
            $$ = new BinaryOperationNode
            {
                Left = (ExpressionNode)$1,
                Operator = "xor",
                Right = (ExpressionNode)$3
            };
        }
    ;

UnaryOperation
    : tkNot Expression
        {
            $$ = new UnaryOperationNode
            {
                Operator = "not",
                Operand = (ExpressionNode)$2
            };
        }
    | tkMinus Expression %prec UNARY_MINUS
        {
            $$ = new UnaryOperationNode
            {
                Operator = "-",
                Operand = (ExpressionNode)$2
            };
        }
    ;

%%

internal ProgramNode? Result { get; set; }

public Parser(AbstractScanner<object, LexLocation> scanner) : base(scanner) { }
