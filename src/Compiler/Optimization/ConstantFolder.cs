using System;
using Compiler.AST;
using Compiler.Semantic;
using SemanticType = Compiler.Semantic.Type;

namespace Compiler.Optimization;

public sealed class ConstantFolder
{
    private static readonly PrimitiveType IntegerType = new(PrimitiveKind.Integer);
    private static readonly PrimitiveType RealType = new(PrimitiveKind.Real);
    private static readonly PrimitiveType BooleanType = new(PrimitiveKind.Boolean);

    public void Optimize(ProgramNode program)
    {
        if (program == null) return;
        OptimizeProgram(program);
    }

    private void OptimizeProgram(ProgramNode program)
    {
        foreach (var declaration in program.Declarations)
        {
            OptimizeDeclaration(declaration);
        }
    }

    private void OptimizeDeclaration(DeclarationNode declaration)
    {
        switch (declaration)
        {
            case VariableDeclarationNode variableDeclaration:
                OptimizeTypeNode(variableDeclaration.Type);
                if (variableDeclaration.InitialValue != null)
                {
                    variableDeclaration.InitialValue = OptimizeExpression(variableDeclaration.InitialValue);
                }
                break;
            case RoutineDeclarationNode routineDeclaration:
                foreach (var parameter in routineDeclaration.Parameters)
                {
                    OptimizeTypeNode(parameter.Type);
                }

                if (routineDeclaration.ReturnType != null)
                {
                    OptimizeTypeNode(routineDeclaration.ReturnType);
                }

                if (routineDeclaration.Body != null)
                {
                    OptimizeBody(routineDeclaration.Body);
                }
                break;
            case TypeDeclarationNode typeDeclaration:
                OptimizeTypeNode(typeDeclaration.Type);
                break;
        }
    }

    private void OptimizeBody(BodyNode body)
    {
        foreach (var declaration in body.Declarations)
        {
            OptimizeDeclaration(declaration);
        }

        body.Statements = OptimizeStatementList(body.Statements);
    }

    private List<StatementNode> OptimizeStatementList(List<StatementNode> statements)
    {
        var optimized = new List<StatementNode>();
        foreach (var statement in statements)
        {
            optimized.AddRange(OptimizeStatement(statement));
        }
        return optimized;
    }

    private List<StatementNode> OptimizeStatement(StatementNode statement)
    {
        switch (statement)
        {
            case AssignmentNode assignment:
                assignment.Target = OptimizeExpression(assignment.Target);
                assignment.Value = OptimizeExpression(assignment.Value);
                return new List<StatementNode> { statement };

            case IfStatementNode ifStatement:
                ifStatement.Condition = OptimizeExpression(ifStatement.Condition);
                ifStatement.ThenBody = OptimizeStatementList(ifStatement.ThenBody);
                ifStatement.ElseBody = OptimizeStatementList(ifStatement.ElseBody);

                if (ifStatement.Condition is BooleanLiteralNode boolLiteral)
                {
                    return boolLiteral.Value
                        ? new List<StatementNode>(ifStatement.ThenBody)
                        : new List<StatementNode>(ifStatement.ElseBody);
                }

                return new List<StatementNode> { ifStatement };

            case WhileLoopNode whileLoop:
                whileLoop.Condition = OptimizeExpression(whileLoop.Condition);
                whileLoop.Body = OptimizeStatementList(whileLoop.Body);

                if (whileLoop.Condition is BooleanLiteralNode loopLiteral && loopLiteral.Value == false)
                {
                    return new List<StatementNode>();
                }

                return new List<StatementNode> { whileLoop };

            case ForLoopNode forLoop:
                forLoop.Range = (RangeNode)OptimizeExpression(forLoop.Range);
                forLoop.Body = OptimizeStatementList(forLoop.Body);
                return new List<StatementNode> { forLoop };

            case ReturnStatementNode returnStatement:
                if (returnStatement.Value != null)
                {
                    returnStatement.Value = OptimizeExpression(returnStatement.Value);
                }
                return new List<StatementNode> { returnStatement };

            case PrintStatementNode printStatement:
                printStatement.Expression = OptimizeExpression(printStatement.Expression);
                return new List<StatementNode> { printStatement };

            default:
                return new List<StatementNode> { statement };
        }
    }

    private void OptimizeTypeNode(TypeNode typeNode)
    {
        switch (typeNode)
        {
            case ArrayTypeNode arrayType:
                arrayType.Size = OptimizeExpression(arrayType.Size);
                OptimizeTypeNode(arrayType.ElementType);
                break;
            case RecordTypeNode recordType:
                foreach (var field in recordType.Fields)
                {
                    OptimizeTypeNode(field.Type);
                }
                break;
        }
    }

    private ExpressionNode OptimizeExpression(ExpressionNode expression)
    {
        switch (expression)
        {
            case BinaryOperationNode binaryOperation:
                binaryOperation.Left = OptimizeExpression(binaryOperation.Left);
                binaryOperation.Right = OptimizeExpression(binaryOperation.Right);
                return TryFoldBinary(binaryOperation);
            case UnaryOperationNode unaryOperation:
                unaryOperation.Operand = OptimizeExpression(unaryOperation.Operand);
                return TryFoldUnary(unaryOperation);
            case ArrayAccessNode arrayAccess:
                arrayAccess.Array = OptimizeExpression(arrayAccess.Array);
                arrayAccess.Index = OptimizeExpression(arrayAccess.Index);
                return arrayAccess;
            case RecordAccessNode recordAccess:
                recordAccess.Record = OptimizeExpression(recordAccess.Record);
                return recordAccess;
            case RoutineCallNode routineCall:
                for (int i = 0; i < routineCall.Arguments.Count; i++)
                {
                    routineCall.Arguments[i] = OptimizeExpression(routineCall.Arguments[i]);
                }
                return routineCall;
            case RangeNode range:
                range.Start = OptimizeExpression(range.Start);
                range.End = OptimizeExpression(range.End);
                return range;
            case ArrayInitializerNode arrayInitializer:
                for (int i = 0; i < arrayInitializer.Elements.Count; i++)
                {
                    arrayInitializer.Elements[i] = OptimizeExpression(arrayInitializer.Elements[i]);
                }
                return arrayInitializer;
            default:
                return expression;
        }
    }

    private ExpressionNode TryFoldBinary(BinaryOperationNode binaryOperation)
    {
        if (!TryGetLiteralValue(binaryOperation.Left, out var left) ||
            !TryGetLiteralValue(binaryOperation.Right, out var right))
        {
            return binaryOperation;
        }

        if (!TryEvaluateBinary(binaryOperation, left, right, out var result))
        {
            return binaryOperation;
        }

        return CreateLiteralExpression(result, binaryOperation, binaryOperation.Type);
    }

    private ExpressionNode TryFoldUnary(UnaryOperationNode unaryOperation)
    {
        if (!TryGetLiteralValue(unaryOperation.Operand, out var operand))
        {
            return unaryOperation;
        }

        if (!TryEvaluateUnary(unaryOperation, operand, out var result))
        {
            return unaryOperation;
        }

        var resultType = unaryOperation.Type ?? unaryOperation.Operand.Type;
        return CreateLiteralExpression(result, unaryOperation, resultType);
    }

    private static bool TryEvaluateBinary(BinaryOperationNode operation, LiteralValue left, LiteralValue right, out object result)
    {
        result = default!;
        return operation.Operator switch
        {
            "+" or "-" or "*" or "/" => TryEvaluateArithmetic(operation, left, right, out result),
            "=" or "<>" => TryEvaluateEquality(operation.Operator, left, right, out result),
            "<" or ">" or "<=" or ">=" => TryEvaluateRelational(operation.Operator, left, right, out result),
            "and" or "or" => TryEvaluateLogical(operation.Operator, left, right, out result),
            _ => false
        };
    }

    private static bool TryEvaluateUnary(UnaryOperationNode operation, LiteralValue operand, out object result)
    {
        result = default!;
        switch (operation.Operator)
        {
            case "-":
                if (TryGetNumeric(operand, out var numeric))
                {
                    result = numeric.IsReal ? -numeric.DoubleValue : -numeric.LongValue;
                    return true;
                }
                break;
            case "not":
                if (TryGetBoolean(operand, out var boolValue))
                {
                    result = !boolValue;
                    return true;
                }
                break;
        }
        return false;
    }

    private static bool TryEvaluateArithmetic(BinaryOperationNode operation, LiteralValue left, LiteralValue right, out object result)
    {
        result = default!;
        if (!TryGetNumeric(left, out var leftNumeric) || !TryGetNumeric(right, out var rightNumeric))
        {
            return false;
        }

        bool expectReal = operation.Type is PrimitiveType { Kind: PrimitiveKind.Real } || leftNumeric.IsReal || rightNumeric.IsReal;

        if (expectReal)
        {
            double l = leftNumeric.DoubleValue;
            double r = rightNumeric.DoubleValue;
            if (operation.Operator == "/" && Math.Abs(r) < double.Epsilon)
            {
                return false;
            }

            result = operation.Operator switch
            {
                "+" => l + r,
                "-" => l - r,
                "*" => l * r,
                "/" => l / r,
                _ => 0d
            };
            return true;
        }
        else
        {
            long l = leftNumeric.LongValue;
            long r = rightNumeric.LongValue;
            if (operation.Operator == "/" && r == 0)
            {
                return false;
            }

            result = operation.Operator switch
            {
                "+" => l + r,
                "-" => l - r,
                "*" => l * r,
                "/" => l / r,
                _ => 0L
            };
            return true;
        }
    }

    private static bool TryEvaluateEquality(string op, LiteralValue left, LiteralValue right, out object result)
    {
        result = default!;
        if (TryGetNumeric(left, out var leftNumeric) && TryGetNumeric(right, out var rightNumeric))
        {
            double l = leftNumeric.DoubleValue;
            double r = rightNumeric.DoubleValue;
            bool comparison = op == "=" ? l == r : l != r;
            result = comparison;
            return true;
        }

        if (TryGetBoolean(left, out var leftBool) && TryGetBoolean(right, out var rightBool))
        {
            bool comparison = op == "=" ? leftBool == rightBool : leftBool != rightBool;
            result = comparison;
            return true;
        }

        if (left.Value is long ll && right.Value is long rr)
        {
            bool comparison = op == "=" ? ll == rr : ll != rr;
            result = comparison;
            return true;
        }

        return false;
    }

    private static bool TryEvaluateRelational(string op, LiteralValue left, LiteralValue right, out object result)
    {
        result = default!;
        if (!TryGetNumeric(left, out var leftNumeric) || !TryGetNumeric(right, out var rightNumeric))
        {
            return false;
        }

        double l = leftNumeric.DoubleValue;
        double r = rightNumeric.DoubleValue;

        bool comparison = op switch
        {
            "<" => l < r,
            ">" => l > r,
            "<=" => l <= r,
            ">=" => l >= r,
            _ => false
        };

        result = comparison;
        return true;
    }

    private static bool TryEvaluateLogical(string op, LiteralValue left, LiteralValue right, out object result)
    {
        result = default!;
        if (!TryGetBoolean(left, out var leftBool) || !TryGetBoolean(right, out var rightBool))
        {
            return false;
        }

        result = op switch
        {
            "and" => leftBool && rightBool,
            "or" => leftBool || rightBool,
            _ => false
        };
        return true;
    }

    private static ExpressionNode CreateLiteralExpression(object value, ExpressionNode source, SemanticType? resultType)
    {
        ExpressionNode literal = value switch
        {
            bool b => new BooleanLiteralNode { Value = b },
            long l => new IntegerLiteralNode { Value = l },
            int i => new IntegerLiteralNode { Value = i },
            double d => new RealLiteralNode { Value = d },
            float f => new RealLiteralNode { Value = f },
            _ => throw new InvalidOperationException("Unsupported literal value type for constant folding.")
        };

        CopyMetadata(source, literal);
        literal.Type = resultType ?? DetermineTypeFromValue(value);
        return literal;
    }

    private static SemanticType DetermineTypeFromValue(object value)
    {
        return value switch
        {
            bool => BooleanType,
            long => IntegerType,
            int => IntegerType,
            double => RealType,
            float => RealType,
            _ => IntegerType
        };
    }

    private static void CopyMetadata(AstNode source, AstNode target)
    {
        target.Line = source.Line;
        target.Column = source.Column;
        target.Scope = source.Scope;
        target.ScopeLevel = source.ScopeLevel;
    }

    private static bool TryGetLiteralValue(ExpressionNode expression, out LiteralValue literal)
    {
        switch (expression)
        {
            case IntegerLiteralNode integerLiteral:
                literal = new LiteralValue(integerLiteral.Value, expression.Type as PrimitiveType ?? IntegerType);
                return true;
            case RealLiteralNode realLiteral:
                literal = new LiteralValue(realLiteral.Value, expression.Type as PrimitiveType ?? RealType);
                return true;
            case BooleanLiteralNode booleanLiteral:
                literal = new LiteralValue(booleanLiteral.Value, expression.Type as PrimitiveType ?? BooleanType);
                return true;
            default:
                literal = default;
                return false;
        }
    }

    private static bool TryGetNumeric(LiteralValue literal, out NumericValue numeric)
    {
        switch (literal.Value)
        {
            case double d:
                numeric = new NumericValue(d);
                return true;
            case float f:
                numeric = new NumericValue(f);
                return true;
            case long l:
                numeric = new NumericValue(l);
                return true;
            case int i:
                numeric = new NumericValue(i);
                return true;
            default:
                numeric = default;
                return false;
        }
    }

    private static bool TryGetBoolean(LiteralValue literal, out bool value)
    {
        switch (literal.Value)
        {
            case bool b:
                value = b;
                return true;
            case long l:
                value = l != 0;
                return true;
            case int i:
                value = i != 0;
                return true;
            default:
                value = false;
                return false;
        }
    }

    private readonly record struct LiteralValue(object Value, PrimitiveType? PrimitiveType);

    private readonly struct NumericValue
    {
        public double DoubleValue { get; }
        public long LongValue { get; }
        public bool IsReal { get; }

        public NumericValue(double value)
        {
            DoubleValue = value;
            LongValue = (long)Math.Round(value);
            IsReal = true;
        }

        public NumericValue(long value)
        {
            DoubleValue = value;
            LongValue = value;
            IsReal = false;
        }
    }
}
