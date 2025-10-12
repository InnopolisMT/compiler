namespace Compiler.AST;

// ============================================================================
// Базовые узлы AST
// ============================================================================

/// <summary>
/// Базовый класс для всех узлов AST
/// </summary>
public abstract class AstNode
{
    public int Line { get; set; }
    public int Column { get; set; }
}

// ============================================================================
// Программа и объявления
// ============================================================================

/// <summary>
/// Корневой узел - программа
/// </summary>
public class ProgramNode : AstNode
{
    public List<DeclarationNode> Declarations { get; set; } = new();
}

/// <summary>
/// Базовый класс для всех объявлений
/// </summary>
public abstract class DeclarationNode : AstNode
{
}

/// <summary>
/// Объявление переменной: var x : integer is 42
/// </summary>
public class VariableDeclarationNode : DeclarationNode
{
    public string Name { get; set; } = "";
    public TypeNode Type { get; set; } = null!;
    public ExpressionNode? InitialValue { get; set; }
}

/// <summary>
/// Объявление типа: type IntArray is array[5] integer
/// </summary>
public class TypeDeclarationNode : DeclarationNode
{
    public string Name { get; set; } = "";
    public TypeNode Type { get; set; } = null!;
}

/// <summary>
/// Объявление подпрограммы: routine main() ... end
/// </summary>
public class RoutineDeclarationNode : DeclarationNode
{
    public string Name { get; set; } = "";
    public List<ParameterNode> Parameters { get; set; } = new();
    public TypeNode? ReturnType { get; set; }
    public BodyNode Body { get; set; } = null!;
}

/// <summary>
/// Параметр подпрограммы
/// </summary>
public class ParameterNode : AstNode
{
    public string Name { get; set; } = "";
    public TypeNode Type { get; set; } = null!;
}

/// <summary>
/// Тело подпрограммы (содержит объявления и операторы)
/// </summary>
public class BodyNode : AstNode
{
    public List<DeclarationNode> Declarations { get; set; } = new();
    public List<StatementNode> Statements { get; set; } = new();
}

// ============================================================================
// Типы
// ============================================================================

/// <summary>
/// Базовый класс для типов
/// </summary>
public abstract class TypeNode : AstNode
{
}

/// <summary>
/// Примитивный тип (integer, real, boolean)
/// </summary>
public class PrimitiveTypeNode : TypeNode
{
    public string TypeName { get; set; } = "";
}

/// <summary>
/// Пользовательский тип (ссылка на объявленный тип)
/// </summary>
public class UserTypeNode : TypeNode
{
    public string TypeName { get; set; } = "";
}

/// <summary>
/// Тип массива: array[size] type
/// </summary>
public class ArrayTypeNode : TypeNode
{
    public ExpressionNode Size { get; set; } = null!;
    public TypeNode ElementType { get; set; } = null!;
}

/// <summary>
/// Тип записи: record ... end
/// </summary>
public class RecordTypeNode : TypeNode
{
    public List<VariableDeclarationNode> Fields { get; set; } = new();
}

// ============================================================================
// Операторы
// ============================================================================

/// <summary>
/// Базовый класс для всех операторов
/// </summary>
public abstract class StatementNode : AstNode
{
}

/// <summary>
/// Оператор присваивания: x := value
/// </summary>
public class AssignmentNode : StatementNode
{
    public ExpressionNode Target { get; set; } = null!;
    public ExpressionNode Value { get; set; } = null!;
}

/// <summary>
/// Условный оператор: if (condition) then ... else ... end
/// </summary>
public class IfStatementNode : StatementNode
{
    public ExpressionNode Condition { get; set; } = null!;
    public List<StatementNode> ThenBody { get; set; } = new();
    public List<StatementNode> ElseBody { get; set; } = new();
}

/// <summary>
/// Цикл while: while (condition) loop ... end
/// </summary>
public class WhileLoopNode : StatementNode
{
    public ExpressionNode Condition { get; set; } = null!;
    public List<StatementNode> Body { get; set; } = new();
}

/// <summary>
/// Цикл for: for i in range loop ... end
/// </summary>
public class ForLoopNode : StatementNode
{
    public string Variable { get; set; } = "";
    public ExpressionNode Range { get; set; } = null!;
    public bool IsReverse { get; set; }
    public List<StatementNode> Body { get; set; } = new();
}

/// <summary>
/// Оператор возврата: return expression
/// </summary>
public class ReturnStatementNode : StatementNode
{
    public ExpressionNode? Value { get; set; }
}

/// <summary>
/// Оператор вывода: print expression
/// </summary>
public class PrintStatementNode : StatementNode
{
    public ExpressionNode Expression { get; set; } = null!;
}

// ============================================================================
// Выражения
// ============================================================================

/// <summary>
/// Базовый класс для всех выражений
/// </summary>
public abstract class ExpressionNode : AstNode
{
}

/// <summary>
/// Бинарная операция: left op right
/// </summary>
public class BinaryOperationNode : ExpressionNode
{
    public ExpressionNode Left { get; set; } = null!;
    public string Operator { get; set; } = "";
    public ExpressionNode Right { get; set; } = null!;
}

/// <summary>
/// Унарная операция: op operand
/// </summary>
public class UnaryOperationNode : ExpressionNode
{
    public string Operator { get; set; } = "";
    public ExpressionNode Operand { get; set; } = null!;
}

/// <summary>
/// Литерал - целое число
/// </summary>
public class IntegerLiteralNode : ExpressionNode
{
    public long Value { get; set; }
}

/// <summary>
/// Литерал - вещественное число
/// </summary>
public class RealLiteralNode : ExpressionNode
{
    public double Value { get; set; }
}

/// <summary>
/// Литерал - логическое значение
/// </summary>
public class BooleanLiteralNode : ExpressionNode
{
    public bool Value { get; set; }
}

/// <summary>
/// Идентификатор (переменная)
/// </summary>
public class IdentifierNode : ExpressionNode
{
    public string Name { get; set; } = "";
}

/// <summary>
/// Доступ к элементу массива: array[index]
/// </summary>
public class ArrayAccessNode : ExpressionNode
{
    public ExpressionNode Array { get; set; } = null!;
    public ExpressionNode Index { get; set; } = null!;
}

/// <summary>
/// Доступ к полю записи: record.field
/// </summary>
public class RecordAccessNode : ExpressionNode
{
    public ExpressionNode Record { get; set; } = null!;
    public string FieldName { get; set; } = "";
}

/// <summary>
/// Вызов подпрограммы: routine(args)
/// </summary>
public class RoutineCallNode : ExpressionNode
{
    public string RoutineName { get; set; } = "";
    public List<ExpressionNode> Arguments { get; set; } = new();
}

/// <summary>
/// Диапазон: start .. end
/// </summary>
public class RangeNode : ExpressionNode
{
    public ExpressionNode Start { get; set; } = null!;
    public ExpressionNode End { get; set; } = null!;
}

/// <summary>
/// Инициализация массива: [1, 2, 3]
/// </summary>
public class ArrayInitializerNode : ExpressionNode
{
    public List<ExpressionNode> Elements { get; set; } = new();
}

