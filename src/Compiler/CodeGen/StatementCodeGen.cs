using Compiler.AST;
using Compiler.Semantic;

namespace Compiler.CodeGen;

public class StatementCodeGen
{
    private readonly WasmInstructionBuilder _builder;
    private readonly ExpressionCodeGen _exprGen;
    private readonly MemoryManager _memoryManager;
    private readonly LocalsManager _localsManager;
    private readonly Dictionary<string, int> _functionIndices;
    private readonly Dictionary<string, Semantic.Type?> _functionReturnTypes;
    private Semantic.Type? _currentFunctionReturnType;

    public StatementCodeGen(
        WasmInstructionBuilder builder,
        ExpressionCodeGen exprGen,
        MemoryManager memoryManager,
        LocalsManager localsManager,
        Dictionary<string, int> functionIndices,
        Dictionary<string, Semantic.Type?> functionReturnTypes)
    {
        _builder = builder;
        _exprGen = exprGen;
        _memoryManager = memoryManager;
        _localsManager = localsManager;
        _functionIndices = functionIndices;
        _functionReturnTypes = functionReturnTypes;
    }

    public void SetCurrentFunctionReturnType(Semantic.Type? returnType)
    {
        _currentFunctionReturnType = returnType;
    }

    public void Generate(StatementNode stmt)
    {
        switch (stmt)
        {
            case AssignmentNode assignment:
                GenerateAssignment(assignment);
                break;
            case IfStatementNode ifStmt:
                GenerateIfStatement(ifStmt);
                break;
            case WhileLoopNode whileLoop:
                GenerateWhileLoop(whileLoop);
                break;
            case ForLoopNode forLoop:
                GenerateForLoop(forLoop);
                break;
            case ReturnStatementNode returnStmt:
                GenerateReturn(returnStmt);
                break;
            case PrintStatementNode printStmt:
                GeneratePrint(printStmt);
                break;
            case ExpressionStatementNode exprStmt:
                GenerateExpressionStatement(exprStmt);
                break;
            default:
                throw new InvalidOperationException($"Unknown statement type: {stmt.GetType().Name}");
        }
    }

    private void GenerateAssignment(AssignmentNode node)
    {
        if (node.Target is IdentifierNode ident)
        {
            var local = _localsManager.GetLocal(ident.Name);
            if (local != null && !local.MemoryOffset.HasValue)
            {
                _exprGen.Generate(node.Value);
                
                if (local.Type == WasmValueType.F64 && !IsRealType(node.Value.Type))
                {
                    _builder.F64ConvertI32S();
                }
                else if (local.Type == WasmValueType.I32 && IsRealType(node.Value.Type))
                {
                    _builder.I32TruncF64S();
                }
                
                _builder.LocalSet(local.Index);
                return;
            }
        }

        _exprGen.GenerateAddress(node.Target);
        _exprGen.Generate(node.Value);

        var targetType = node.Target.Type;
        if (IsRealType(targetType))
        {
            if (!IsRealType(node.Value.Type))
            {
                _builder.F64ConvertI32S();
            }
            _builder.F64Store();
        }
        else
        {
            if (IsRealType(node.Value.Type))
            {
                _builder.I32TruncF64S();
            }
            _builder.I32Store();
        }
    }

    private void GenerateIfStatement(IfStatementNode node)
    {
        _exprGen.Generate(node.Condition);

        if (node.ElseBody.Count > 0)
        {
            _builder.If();
            
            foreach (var stmt in node.ThenBody)
            {
                Generate(stmt);
            }
            
            _builder.Else();
            
            foreach (var stmt in node.ElseBody)
            {
                Generate(stmt);
            }
            
            _builder.End();
            
            // If both branches end with return, mark code after as unreachable
            if (EndsWithReturn(node.ThenBody) && EndsWithReturn(node.ElseBody))
            {
                _builder.Unreachable();
            }
        }
        else
        {
            _builder.If();
            
            foreach (var stmt in node.ThenBody)
            {
                Generate(stmt);
            }
            
            _builder.End();
        }
    }
    
    private bool EndsWithReturn(List<StatementNode> statements)
    {
        if (statements.Count == 0)
            return false;
            
        var lastStmt = statements[statements.Count - 1];
        
        if (lastStmt is ReturnStatementNode)
            return true;
            
        // Check if the last statement is an if-else where both branches return
        if (lastStmt is IfStatementNode ifStmt && ifStmt.ElseBody.Count > 0)
        {
            return EndsWithReturn(ifStmt.ThenBody) && EndsWithReturn(ifStmt.ElseBody);
        }
        
        return false;
    }

    private void GenerateWhileLoop(WhileLoopNode node)
    {
        _builder.Block();
        _builder.Loop();

        _exprGen.Generate(node.Condition);
        _builder.I32Eqz();
        _builder.BrIf(1);

        foreach (var stmt in node.Body)
        {
            Generate(stmt);
        }

        _builder.Br(0);
        _builder.End();
        _builder.End();
    }

    private void GenerateForLoop(ForLoopNode node)
    {
        if (node.Range is not RangeNode range)
            throw new InvalidOperationException("For loop range must be a RangeNode");

        var loopVar = _localsManager.GetLocal(node.Variable);
        if (loopVar == null)
        {
            _localsManager.AddLocal(node.Variable, new Semantic.PrimitiveType(Semantic.PrimitiveKind.Integer));
            loopVar = _localsManager.GetLocal(node.Variable);
            if (loopVar == null)
                throw new InvalidOperationException($"Failed to create loop variable '{node.Variable}'");
        }

        _exprGen.Generate(range.Start);
        _builder.LocalSet(loopVar.Index);

        int endLocal = _localsManager.AllocateTemporary(WasmValueType.I32);
        _exprGen.Generate(range.End);
        _builder.LocalSet(endLocal);

        _builder.Block();
        _builder.Loop();

        if (node.IsReverse)
        {
            _builder.LocalGet(loopVar.Index);
            _builder.LocalGet(endLocal);
            _builder.I32LtS();
            _builder.BrIf(1);
        }
        else
        {
            _builder.LocalGet(loopVar.Index);
            _builder.LocalGet(endLocal);
            _builder.I32GtS();
            _builder.BrIf(1);
        }

        foreach (var stmt in node.Body)
        {
            Generate(stmt);
        }

        _builder.LocalGet(loopVar.Index);
        if (node.IsReverse)
        {
            _builder.I32Const(1);
            _builder.I32Sub();
        }
        else
        {
            _builder.I32Const(1);
            _builder.I32Add();
        }
        _builder.LocalSet(loopVar.Index);

        _builder.Br(0);
        _builder.End();
        _builder.End();
    }

    private void GenerateReturn(ReturnStatementNode node)
    {
        if (node.Value != null)
        {
            // Check if we need to force real arithmetic for division
            bool forceRealArithmetic = false;
            if (_currentFunctionReturnType != null && IsRealType(_currentFunctionReturnType))
            {
                forceRealArithmetic = true;
            }
            
            GenerateExpressionWithContext(node.Value, forceRealArithmetic);
            
            // Convert type if necessary (in case the expression didn't already produce the right type)
            if (_currentFunctionReturnType != null && node.Value.Type != null)
            {
                bool returnTypeIsReal = IsRealType(_currentFunctionReturnType);
                bool valueTypeIsReal = IsRealType(node.Value.Type);
                
                if (returnTypeIsReal && !valueTypeIsReal && !forceRealArithmetic)
                {
                    // Need to convert i32 to f64
                    _builder.F64ConvertI32S();
                }
                else if (!returnTypeIsReal && valueTypeIsReal)
                {
                    // Need to convert f64 to i32 (truncate)
                    _builder.I32TruncF64S();
                }
            }
        }
        _builder.Return();
    }
    
    private void GenerateExpressionWithContext(ExpressionNode expr, bool forceRealArithmetic)
    {
        if (!forceRealArithmetic)
        {
            _exprGen.Generate(expr);
            return;
        }
        
        // Handle binary operations with potential integer division
        if (expr is BinaryOperationNode binOp)
        {
            var leftType = binOp.Left.Type;
            var rightType = binOp.Right.Type;
            
            // Recursively generate operands with context
            GenerateExpressionWithContext(binOp.Left, forceRealArithmetic);
            GenerateExpressionWithContext(binOp.Right, forceRealArithmetic);
            
            // For division of two integers in real context, ensure real division
            if (binOp.Operator == "/" && !IsRealType(leftType) && !IsRealType(rightType))
            {
                // Right operand is on top of stack
                var tempRight = _localsManager.AllocateTemporary(WasmValueType.I32);
                _builder.LocalSet(tempRight);
                
                // Convert left (now on top)
                _builder.F64ConvertI32S();
                
                // Convert right
                _builder.LocalGet(tempRight);
                _builder.F64ConvertI32S();
                
                _builder.F64Div();
            }
            else
            {
                // Use normal binary operation generation logic
                bool isReal = IsRealType(leftType) || IsRealType(rightType);
                
                if (isReal)
                {
                    if (!IsRealType(leftType) && !IsRealType(rightType))
                    {
                        var tempRight = _localsManager.AllocateTemporary(WasmValueType.I32);
                        _builder.LocalSet(tempRight);
                        _builder.F64ConvertI32S();
                        _builder.LocalGet(tempRight);
                        _builder.F64ConvertI32S();
                    }
                    else if (!IsRealType(leftType))
                    {
                        var tempRight = _localsManager.AllocateTemporary(WasmValueType.F64);
                        _builder.LocalSet(tempRight);
                        _builder.F64ConvertI32S();
                        _builder.LocalGet(tempRight);
                    }
                    else if (!IsRealType(rightType))
                    {
                        _builder.F64ConvertI32S();
                    }
                    
                    GenerateRealBinaryOp(binOp.Operator);
                }
                else
                {
                    GenerateIntegerBinaryOp(binOp.Operator);
                }
            }
            return;
        }
        
        // For other expression types, use normal generation
        _exprGen.Generate(expr);
    }
    
    private void GenerateIntegerBinaryOp(string op)
    {
        switch (op)
        {
            case "+": _builder.I32Add(); break;
            case "-": _builder.I32Sub(); break;
            case "*": _builder.I32Mul(); break;
            case "/": _builder.I32DivS(); break;
            case "%": _builder.I32RemS(); break;
            case "<": _builder.I32LtS(); break;
            case ">": _builder.I32GtS(); break;
            case "<=": _builder.I32LeS(); break;
            case ">=": _builder.I32GeS(); break;
            case "=": _builder.I32Eq(); break;
            case "/=": _builder.I32Ne(); break;
            case "and": _builder.I32And(); break;
            case "or": _builder.I32Or(); break;
            case "xor": _builder.I32Xor(); break;
            default: throw new InvalidOperationException($"Unknown integer operator: {op}");
        }
    }
    
    private void GenerateRealBinaryOp(string op)
    {
        switch (op)
        {
            case "+": _builder.F64Add(); break;
            case "-": _builder.F64Sub(); break;
            case "*": _builder.F64Mul(); break;
            case "/": _builder.F64Div(); break;
            case "<": _builder.F64Lt(); break;
            case ">": _builder.F64Gt(); break;
            case "<=": _builder.F64Le(); break;
            case ">=": _builder.F64Ge(); break;
            case "=": _builder.F64Eq(); break;
            case "/=": _builder.F64Ne(); break;
            default: throw new InvalidOperationException($"Unknown real operator: {op}");
        }
    }

    private void GeneratePrint(PrintStatementNode node)
    {
        _exprGen.Generate(node.Expression);

        var exprType = node.Expression.Type;
        
        // If type is not set, try to resolve it
        if (exprType == null)
        {
            exprType = _exprGen.GetResolvedType(node.Expression);
        }
        
        // If expression is a routine call, get its return type from _functionReturnTypes
        if (node.Expression is RoutineCallNode routineCall)
        {
            if (_functionReturnTypes.TryGetValue(routineCall.RoutineName, out var returnType))
            {
                exprType = returnType;
            }
        }
        
        // If expression is a record access, resolve the field type correctly
        if (node.Expression is RecordAccessNode recordAccess)
        {
            exprType = _exprGen.GetResolvedType(recordAccess);
        }
        
        string funcName;

        if (IsRealType(exprType))
        {
            funcName = "printReal";
        }
        else if (exprType is PrimitiveType pt && pt.Kind == PrimitiveKind.Boolean)
        {
            funcName = "printBool";
        }
        else
        {
            funcName = "printInt";
        }

        if (!_functionIndices.TryGetValue(funcName, out int funcIndex))
        {
            throw new InvalidOperationException($"Print function '{funcName}' not found");
        }

        _builder.Call(funcIndex);
    }

    private void GenerateExpressionStatement(ExpressionStatementNode node)
    {
        // Generate the expression (typically a function call)
        _exprGen.Generate(node.Expression);
        
        // If the expression has a return value, drop it since we're not using it
        if (node.Expression is RoutineCallNode call)
        {
            // Check if the routine has a return type
            if (_functionReturnTypes.TryGetValue(call.RoutineName, out var returnType) && returnType != null)
            {
                // Drop the unused return value from the stack
                _builder.Drop();
            }
        }
    }

    private bool IsRealType(Semantic.Type? type)
    {
        return type is PrimitiveType pt && pt.Kind == PrimitiveKind.Real;
    }
}

