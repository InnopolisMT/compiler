using Compiler.AST;
using Compiler.Semantic;

namespace Compiler.CodeGen;

public class ExpressionCodeGen
{
    private readonly WasmInstructionBuilder _builder;
    private readonly MemoryManager _memoryManager;
    private readonly LocalsManager _localsManager;
    private readonly Dictionary<string, int> _functionIndices;
    private readonly Dictionary<string, RoutineDeclarationNode> _routineDeclarations;
    private readonly ProgramNode _ast;

    public LocalsManager LocalsManager => _localsManager;

    public ExpressionCodeGen(
        WasmInstructionBuilder builder,
        MemoryManager memoryManager,
        LocalsManager localsManager,
        Dictionary<string, int> functionIndices,
        Dictionary<string, RoutineDeclarationNode> routineDeclarations,
        ProgramNode ast)
    {
        _builder = builder;
        _memoryManager = memoryManager;
        _localsManager = localsManager;
        _functionIndices = functionIndices;
        _routineDeclarations = routineDeclarations;
        _ast = ast;
    }

    public void Generate(ExpressionNode expr)
    {
        switch (expr)
        {
            case IntegerLiteralNode intLit:
                GenerateIntegerLiteral(intLit);
                break;
            case RealLiteralNode realLit:
                GenerateRealLiteral(realLit);
                break;
            case BooleanLiteralNode boolLit:
                GenerateBooleanLiteral(boolLit);
                break;
            case IdentifierNode ident:
                GenerateIdentifier(ident);
                break;
            case BinaryOperationNode binOp:
                GenerateBinaryOperation(binOp);
                break;
            case UnaryOperationNode unOp:
                GenerateUnaryOperation(unOp);
                break;
            case ArrayAccessNode arrayAccess:
                GenerateArrayAccess(arrayAccess);
                break;
            case RecordAccessNode recordAccess:
                GenerateRecordAccess(recordAccess);
                break;
            case RoutineCallNode routineCall:
                GenerateRoutineCall(routineCall);
                break;
            case RangeNode:
                throw new InvalidOperationException("Range nodes should not be evaluated as expressions");
            case ArrayInitializerNode:
                throw new InvalidOperationException("Array initializers are handled separately during initialization");
            default:
                throw new InvalidOperationException($"Unknown expression type: {expr.GetType().Name}");
        }
    }

    private void GenerateIntegerLiteral(IntegerLiteralNode node)
    {
        _builder.I32Const((int)node.Value);
    }

    private void GenerateRealLiteral(RealLiteralNode node)
    {
        _builder.F64Const(node.Value);
    }

    private void GenerateBooleanLiteral(BooleanLiteralNode node)
    {
        _builder.I32Const(node.Value ? 1 : 0);
    }

    private void GenerateIdentifier(IdentifierNode node)
    {
        var local = _localsManager.GetLocal(node.Name);
        if (local != null)
        {
            if (local.MemoryOffset.HasValue)
            {
                _builder.I32Const(local.MemoryOffset.Value);
                if (node.Type is PrimitiveType pt && pt.Kind == PrimitiveKind.Real)
                {
                    _builder.F64Load();
                }
                else
                {
                    _builder.I32Load();
                }
            }
            else
            {
                _builder.LocalGet(local.Index);
            }
            return;
        }

        var globalVar = _memoryManager.GetGlobalVariable(node.Name);
        if (globalVar != null)
        {
            _builder.I32Const(globalVar.Offset);
            if (node.Type is PrimitiveType pt && pt.Kind == PrimitiveKind.Real)
            {
                _builder.F64Load();
            }
            else
            {
                _builder.I32Load();
            }
            return;
        }

        throw new InvalidOperationException($"Variable '{node.Name}' not found");
    }

    private void GenerateBinaryOperation(BinaryOperationNode node)
    {
        var leftType = node.Left.Type;
        var rightType = node.Right.Type;

        Generate(node.Left);
        Generate(node.Right);

        // Division always produces f64
        bool isDivision = node.Operator == "/";
        bool isReal = IsRealType(leftType) || IsRealType(rightType) || isDivision;

        if (isReal)
        {
            // Need to convert both operands to f64 if they are integers
            if (!IsRealType(leftType) && !IsRealType(rightType))
            {
                // Both are integers, need to convert both
                // Right operand is on top of stack
                var tempRight = _localsManager.AllocateTemporary(WasmValueType.I32);
                _builder.LocalSet(tempRight);
                
                // Convert left (now on top)
                _builder.F64ConvertI32S();
                
                // Convert right
                _builder.LocalGet(tempRight);
                _builder.F64ConvertI32S();
            }
            else if (!IsRealType(leftType))
            {
                // Left is integer, right is real
                var tempRight = _localsManager.AllocateTemporary(WasmValueType.F64);
                _builder.LocalSet(tempRight);
                _builder.F64ConvertI32S();
                _builder.LocalGet(tempRight);
            }
            else if (!IsRealType(rightType))
            {
                // Left is real, right is integer
                _builder.F64ConvertI32S();
            }
            // else both are real, no conversion needed

            GenerateRealBinaryOperation(node.Operator);
        }
        else
        {
            GenerateIntegerBinaryOperation(node.Operator);
        }
    }

    private void GenerateIntegerBinaryOperation(string op)
    {
        switch (op)
        {
            case "+":
                _builder.I32Add();
                break;
            case "-":
                _builder.I32Sub();
                break;
            case "*":
                _builder.I32Mul();
                break;
            case "/":
                _builder.I32DivS();
                break;
            case "%":
                _builder.I32RemS();
                break;
            case "<":
                _builder.I32LtS();
                break;
            case ">":
                _builder.I32GtS();
                break;
            case "<=":
                _builder.I32LeS();
                break;
            case ">=":
                _builder.I32GeS();
                break;
            case "=":
                _builder.I32Eq();
                break;
            case "/=":
                _builder.I32Ne();
                break;
            case "and":
                _builder.I32And();
                break;
            case "or":
                _builder.I32Or();
                break;
            case "xor":
                _builder.I32Xor();
                break;
            default:
                throw new InvalidOperationException($"Unknown integer binary operator: {op}");
        }
    }

    private void GenerateRealBinaryOperation(string op)
    {
        switch (op)
        {
            case "+":
                _builder.F64Add();
                break;
            case "-":
                _builder.F64Sub();
                break;
            case "*":
                _builder.F64Mul();
                break;
            case "/":
                _builder.F64Div();
                break;
            case "<":
                _builder.F64Lt();
                break;
            case ">":
                _builder.F64Gt();
                break;
            case "<=":
                _builder.F64Le();
                break;
            case ">=":
                _builder.F64Ge();
                break;
            case "=":
                _builder.F64Eq();
                break;
            case "/=":
                _builder.F64Ne();
                break;
            default:
                throw new InvalidOperationException($"Unknown or unsupported real binary operator: {op}");
        }
    }

    private void GenerateUnaryOperation(UnaryOperationNode node)
    {
        Generate(node.Operand);

        switch (node.Operator)
        {
            case "-":
                if (IsRealType(node.Operand.Type))
                {
                    _builder.F64Const(-1.0);
                    _builder.F64Mul();
                }
                else
                {
                    _builder.I32Const(-1);
                    _builder.I32Mul();
                }
                break;
            case "not":
                _builder.I32Eqz();
                break;
            default:
                throw new InvalidOperationException($"Unknown unary operator: {node.Operator}");
        }
    }

    private void GenerateArrayAccess(ArrayAccessNode node)
    {
        GenerateAddress(node);

        var resolvedType = GetResolvedType(node.Array);
        var arrayType = resolvedType as ArrayType;
        if (arrayType == null)
            throw new InvalidOperationException($"Array access on non-array type (got {resolvedType?.GetType().Name ?? "null"})");

        var elementType = arrayType.ElementType;
        if (IsRealType(elementType))
        {
            _builder.F64Load();
        }
        else
        {
            _builder.I32Load();
        }
    }

    private void GenerateRecordAccess(RecordAccessNode node)
    {
        GenerateAddress(node);

        var resolvedType = GetResolvedType(node.Record);
        var recordType = resolvedType as RecordType;
        if (recordType == null)
            throw new InvalidOperationException($"Record access on non-record type (got {resolvedType?.GetType().Name ?? "null"})");

        if (!recordType.Fields.TryGetValue(node.FieldName, out var fieldType))
            throw new InvalidOperationException($"Field '{node.FieldName}' not found in record");

        if (IsRealType(fieldType))
        {
            _builder.F64Load();
        }
        else
        {
            _builder.I32Load();
        }
    }

    public void GenerateAddress(ExpressionNode expr)
    {
        switch (expr)
        {
            case IdentifierNode ident:
                GenerateIdentifierAddress(ident);
                break;
            case ArrayAccessNode arrayAccess:
                GenerateArrayAccessAddress(arrayAccess);
                break;
            case RecordAccessNode recordAccess:
                GenerateRecordAccessAddress(recordAccess);
                break;
            default:
                throw new InvalidOperationException($"Cannot get address of expression type: {expr.GetType().Name}");
        }
    }

    private void GenerateIdentifierAddress(IdentifierNode node)
    {
        var local = _localsManager.GetLocal(node.Name);
        if (local != null)
        {
            if (local.IsReference)
            {
                // For reference parameters, the local already contains the address
                _builder.LocalGet(local.Index);
            }
            else if (local.MemoryOffset.HasValue)
            {
                _builder.I32Const(local.MemoryOffset.Value);
            }
            else
            {
                throw new InvalidOperationException($"Cannot get address of WASM local variable '{node.Name}' - it's stored in a local, not memory");
            }
            return;
        }

        var globalVar = _memoryManager.GetGlobalVariable(node.Name);
        if (globalVar != null)
        {
            _builder.I32Const(globalVar.Offset);
            return;
        }

        throw new InvalidOperationException($"Variable '{node.Name}' not found");
    }

    private void GenerateArrayAccessAddress(ArrayAccessNode node)
    {
        GenerateAddress(node.Array);

        var resolvedType = GetResolvedType(node.Array);
        var arrayType = resolvedType as ArrayType;
        if (arrayType == null)
            throw new InvalidOperationException($"Array access on non-array type (got {resolvedType?.GetType().Name ?? "null"})");

        int elementSize = _memoryManager.CalculateTypeSize(arrayType.ElementType);

        // Pascal-like 1-based indexing: array[1] is the first element
        Generate(node.Index);
        _builder.I32Const(1);
        _builder.I32Sub(); // index - 1
        _builder.I32Const(elementSize);
        _builder.I32Mul();
        _builder.I32Add();
    }

    private void GenerateRecordAccessAddress(RecordAccessNode node)
    {
        GenerateAddress(node.Record);

        var resolvedType = GetResolvedType(node.Record);
        var recordType = resolvedType as RecordType;
        if (recordType == null)
            throw new InvalidOperationException($"Record access on non-record type (got {resolvedType?.GetType().Name ?? "null"})");

        int fieldOffset = _memoryManager.CalculateFieldOffset(recordType, node.FieldName);
        if (fieldOffset > 0)
        {
            _builder.I32Const(fieldOffset);
            _builder.I32Add();
        }
    }

    private void GenerateRoutineCall(RoutineCallNode node)
    {
        // Get routine declaration to check parameter types
        RoutineDeclarationNode? routine = null;
        if (_routineDeclarations.TryGetValue(node.RoutineName, out var routineDecl))
        {
            routine = routineDecl;
        }

        // Generate arguments - pass complex types by reference
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            var arg = node.Arguments[i];
            
            // Check if this parameter is a complex type
            bool shouldPassByReference = false;
            if (routine != null && i < routine.Parameters.Count)
            {
                var paramType = ResolveType(routine.Parameters[i].Type);
                shouldPassByReference = _memoryManager.IsComplexType(paramType);
            }
            
            if (shouldPassByReference)
            {
                // Pass address for arrays and records
                GenerateAddress(arg);
            }
            else
            {
                // Pass value for primitive types
                Generate(arg);
            }
        }

        if (!_functionIndices.TryGetValue(node.RoutineName, out int funcIndex))
        {
            throw new InvalidOperationException($"Function '{node.RoutineName}' not found");
        }

        _builder.Call(funcIndex);
    }

    private bool IsRealType(Semantic.Type? type)
    {
        return type is PrimitiveType pt && pt.Kind == PrimitiveKind.Real;
    }

    private Semantic.Type ResolveToActualType(Semantic.Type? type)
    {
        if (type == null)
            throw new InvalidOperationException("Cannot resolve null type");

        return type;
    }

    public Semantic.Type? GetResolvedType(ExpressionNode expr)
    {
        if (expr is IdentifierNode ident)
        {
            // Try to get from LocalsManager first (for local variables)
            var local = _localsManager.GetLocal(ident.Name);
            if (local != null && local.OriginalType != null)
            {
                return local.OriginalType;
            }
            
            // Check global variables
            var globalVar = _memoryManager.GetGlobalVariable(ident.Name);
            if (globalVar != null && globalVar.OriginalType != null)
            {
                return globalVar.OriginalType;
            }
            
            // Try to get from Symbol (fallback)
            if (ident.Symbol != null && ident.Symbol.Type != null)
            {
                return ident.Symbol.Type;
            }
        }
        
        if (expr is ArrayAccessNode arrayAccess)
        {
            var baseType = GetResolvedType(arrayAccess.Array);
            if (baseType is ArrayType at)
            {
                return at.ElementType;
            }
        }

        if (expr is RecordAccessNode recordAccess)
        {
            var baseType = GetResolvedType(recordAccess.Record);
            if (baseType is RecordType rt && rt.Fields.TryGetValue(recordAccess.FieldName, out var fieldType))
            {
                return fieldType;
            }
        }

        return expr.Type;
    }

    public Semantic.Type ResolveType(TypeNode typeNode)
    {
        switch (typeNode)
        {
            case PrimitiveTypeNode primitive:
                return primitive.TypeName switch
                {
                    "integer" => new PrimitiveType(PrimitiveKind.Integer),
                    "real" => new PrimitiveType(PrimitiveKind.Real),
                    "boolean" => new PrimitiveType(PrimitiveKind.Boolean),
                    _ => throw new InvalidOperationException($"Unknown primitive type: {primitive.TypeName}")
                };

            case ArrayTypeNode arrayType:
                var elementType = ResolveType(arrayType.ElementType);
                int size = EvaluateConstantExpression(arrayType.Size);
                return new ArrayType(elementType, size);

            case RecordTypeNode recordType:
                var fields = new Dictionary<string, Semantic.Type>();
                foreach (var field in recordType.Fields)
                {
                    fields[field.Name] = ResolveType(field.Type);
                }
                return new RecordType(fields);

            case UserTypeNode userType:
                var typeDecl = _ast.Declarations.OfType<TypeDeclarationNode>()
                    .FirstOrDefault(d => d.Name == userType.TypeName);
                if (typeDecl == null)
                    throw new InvalidOperationException($"Type '{userType.TypeName}' not found");
                return ResolveType(typeDecl.Type);

            default:
                throw new InvalidOperationException($"Unknown type node: {typeNode.GetType().Name}");
        }
    }

    private int EvaluateConstantExpression(ExpressionNode expr)
    {
        return expr switch
        {
            IntegerLiteralNode intLit => (int)intLit.Value,
            BinaryOperationNode binOp => EvaluateBinaryOperation(binOp),
            _ => throw new InvalidOperationException($"Cannot evaluate non-constant expression: {expr.GetType().Name}")
        };
    }

    private int EvaluateBinaryOperation(BinaryOperationNode node)
    {
        int left = EvaluateConstantExpression(node.Left);
        int right = EvaluateConstantExpression(node.Right);

        return node.Operator switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => left / right,
            "%" => left % right,
            _ => throw new InvalidOperationException($"Cannot evaluate operator: {node.Operator}")
        };
    }
}

