namespace Compiler.AST;

public abstract class AstNode
{
    public int Line { get; set; }
    public int Column { get; set; }
}

public class ProgramNode : AstNode
{
    public List<DeclarationNode> Declarations { get; set; } = new();
}

public abstract class DeclarationNode : AstNode
{
}

public class VariableDeclarationNode : DeclarationNode
{
    public string Name { get; set; } = "";
    public TypeNode Type { get; set; } = null!;
    public ExpressionNode? InitialValue { get; set; }
}
public class TypeDeclarationNode : DeclarationNode
{
    public string Name { get; set; } = "";
    public TypeNode Type { get; set; } = null!;
}
public class RoutineDeclarationNode : DeclarationNode
{
    public string Name { get; set; } = "";
    public List<ParameterNode> Parameters { get; set; } = new();
    public TypeNode? ReturnType { get; set; }
    public BodyNode Body { get; set; } = null!;
}
public class ParameterNode : AstNode
{
    public string Name { get; set; } = "";
    public TypeNode Type { get; set; } = null!;
}
public class BodyNode : AstNode
{
    public List<DeclarationNode> Declarations { get; set; } = new();
    public List<StatementNode> Statements { get; set; } = new();
}
public abstract class TypeNode : AstNode
{
}
public class PrimitiveTypeNode : TypeNode
{
    public string TypeName { get; set; } = "";
}

public class UserTypeNode : TypeNode
{
    public string TypeName { get; set; } = "";
}
public class ArrayTypeNode : TypeNode
{
    public ExpressionNode Size { get; set; } = null!;
    public TypeNode ElementType { get; set; } = null!;
}

public class RecordTypeNode : TypeNode
{
    public List<VariableDeclarationNode> Fields { get; set; } = new();
}

public abstract class StatementNode : AstNode
{
}
public class AssignmentNode : StatementNode
{
    public ExpressionNode Target { get; set; } = null!;
    public ExpressionNode Value { get; set; } = null!;
}

public class IfStatementNode : StatementNode
{
    public ExpressionNode Condition { get; set; } = null!;
    public List<StatementNode> ThenBody { get; set; } = new();
    public List<StatementNode> ElseBody { get; set; } = new();
}

public class WhileLoopNode : StatementNode
{
    public ExpressionNode Condition { get; set; } = null!;
    public List<StatementNode> Body { get; set; } = new();
}

public class ForLoopNode : StatementNode
{
    public string Variable { get; set; } = "";
    public ExpressionNode Range { get; set; } = null!;
    public bool IsReverse { get; set; }
    public List<StatementNode> Body { get; set; } = new();
}

public class ReturnStatementNode : StatementNode
{
    public ExpressionNode? Value { get; set; }
}
public class PrintStatementNode : StatementNode
{
    public ExpressionNode Expression { get; set; } = null!;
}

public abstract class ExpressionNode : AstNode
{
}

public class BinaryOperationNode : ExpressionNode
{
    public ExpressionNode Left { get; set; } = null!;
    public string Operator { get; set; } = "";
    public ExpressionNode Right { get; set; } = null!;
}

public class UnaryOperationNode : ExpressionNode
{
    public string Operator { get; set; } = "";
    public ExpressionNode Operand { get; set; } = null!;
}

public class IntegerLiteralNode : ExpressionNode
{
    public long Value { get; set; }
}

public class RealLiteralNode : ExpressionNode
{
    public double Value { get; set; }
}

public class BooleanLiteralNode : ExpressionNode
{
    public bool Value { get; set; }
}

public class IdentifierNode : ExpressionNode
{
    public string Name { get; set; } = "";
}
public class ArrayAccessNode : ExpressionNode
{
    public ExpressionNode Array { get; set; } = null!;
    public ExpressionNode Index { get; set; } = null!;
}

public class RecordAccessNode : ExpressionNode
{
    public ExpressionNode Record { get; set; } = null!;
    public string FieldName { get; set; } = "";
}

public class RoutineCallNode : ExpressionNode
{
    public string RoutineName { get; set; } = "";
    public List<ExpressionNode> Arguments { get; set; } = new();
}

public class RangeNode : ExpressionNode
{
    public ExpressionNode Start { get; set; } = null!;
    public ExpressionNode End { get; set; } = null!;
}

public class ArrayInitializerNode : ExpressionNode
{
    public List<ExpressionNode> Elements { get; set; } = new();
}

