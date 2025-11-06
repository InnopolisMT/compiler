using Compiler.AST;
using System.Collections.Generic;
using System.Linq;

namespace Compiler.Semantic;

public class SemanticAnalyzer
{
    private static readonly PrimitiveType BooleanType = new(PrimitiveKind.Boolean);
    private static readonly PrimitiveType IntegerType = new(PrimitiveKind.Integer);
    private static readonly PrimitiveType RealType = new(PrimitiveKind.Real);

    private readonly SymbolTable _symbolTable;
    private readonly List<SemanticError> _errors;
    private RoutineDeclarationNode? _currentRoutine;
    private readonly HashSet<string> _typeResolutionStack;
    private readonly Dictionary<string, RoutineDeclarationNode> _forwardDeclarations;

    public IReadOnlyList<SemanticError> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;
    public SymbolTable SymbolTable => _symbolTable;

    private static bool AreCompatible(Type left, Type right) => left.IsCompatibleWith(right) || right.IsCompatibleWith(left);

    public SemanticAnalyzer()
    {
        _symbolTable = new SymbolTable();
        _errors = new List<SemanticError>();
        _typeResolutionStack = new HashSet<string>();
        _forwardDeclarations = new Dictionary<string, RoutineDeclarationNode>();
    }

    public string FormatErrors(string? fileName = null)
    {
        var result = new System.Text.StringBuilder();
        foreach (var error in _errors)
        {
            result.AppendLine(error.Format(fileName));
        }
        return result.ToString();
    }

    public void Analyze(ProgramNode program)
    {
        _symbolTable.Reset();
        _errors.Clear();
        _currentRoutine = null;
        _typeResolutionStack.Clear();
        _forwardDeclarations.Clear();

        Pass1_Declarations(program);

        if (!HasErrors)
        {
            Pass2_TypeChecking(program);
        }
    }

    private void Pass1_Declarations(ProgramNode program)
    {
        foreach (var declaration in program.Declarations)
        {
            switch (declaration)
            {
                case TypeDeclarationNode typeDecl:
                    ProcessTypeDeclaration(typeDecl);
                    break;
                case RoutineDeclarationNode routineDecl:
                    ProcessRoutineDeclaration(routineDecl);
                    break;
            }
        }
        Pass1_CheckForwardDeclarations(program);
    }

    private void Pass1_CheckForwardDeclarations(ProgramNode program)
    {
        // Check for forward declarations that have no full implementation
        // (if a full implementation was found, it would have been removed from _forwardDeclarations in ProcessRoutineDeclaration)
        foreach (var forwardName in _forwardDeclarations.Keys)
        {
            var forwardDecl = _forwardDeclarations[forwardName];
            AddError(forwardDecl.Line, forwardDecl.Column,
                $"Forward declaration of routine '{forwardName}' has no full definition");
        }
    }

    private bool IsFullDeclaration(RoutineDeclarationNode routine)
    {
        // If Body is not null, it means the routine has "is ... end" block, so it's a full declaration
        // If Body is null, it's just a signature without implementation - forward declaration
        return routine.Body != null;
    }

    private bool SignaturesMatch(RoutineDeclarationNode forward, RoutineDeclarationNode full)
    {
        if (forward.Parameters.Count != full.Parameters.Count)
        {
            return false;
        }

        for (int i = 0; i < forward.Parameters.Count; i++)
        {
            var forwardParam = forward.Parameters[i];
            var fullParam = full.Parameters[i];

            if (forwardParam.Name != fullParam.Name)
            {
                return false;
            }

            var forwardParamType = ResolveTypeNode(forwardParam.Type);
            var fullParamType = ResolveTypeNode(fullParam.Type);

            if (forwardParamType == null || fullParamType == null)
            {
                return false;
            }

            if (!forwardParamType.Equals(fullParamType))
            {
                return false;
            }
        }

        Type? forwardReturnType = null;
        if (forward.ReturnType != null)
        {
            forwardReturnType = ResolveTypeNode(forward.ReturnType);
        }

        Type? fullReturnType = null;
        if (full.ReturnType != null)
        {
            fullReturnType = ResolveTypeNode(full.ReturnType);
        }

        if (forwardReturnType == null && fullReturnType == null)
        {
            return true;
        }

        if (forwardReturnType == null || fullReturnType == null)
        {
            return false;
        }

        return forwardReturnType.Equals(fullReturnType);
    }

    private void ProcessTypeDeclaration(TypeDeclarationNode typeDecl)
    {
        AnnotateScope(typeDecl);

        if (_symbolTable.IsDefinedLocally(typeDecl.Name))
        {
            AddError(typeDecl.Line, typeDecl.Column, $"Type '{typeDecl.Name}' is already defined in this scope");
            return;
        }

        if (_typeResolutionStack.Contains(typeDecl.Name))
        {
            AddError(typeDecl.Line, typeDecl.Column,
                $"Circular type definition detected for type '{typeDecl.Name}'");
            return;
        }

        _typeResolutionStack.Add(typeDecl.Name);
        var type = ResolveTypeNode(typeDecl.Type);
        _typeResolutionStack.Remove(typeDecl.Name);

        if (type == null)
        {
            return;
        }

        var symbol = new Symbol(typeDecl.Name, SymbolKind.Type, type)
        {
            DeclarationNode = typeDecl,
            Scope = _symbolTable.CurrentScope
        };

        symbol.CodeGenInfo!.IsGlobal = _symbolTable.CurrentLevel == 0;
        symbol.CodeGenInfo!.IsLocal = _symbolTable.CurrentLevel > 0;

        if (!_symbolTable.Enter(typeDecl.Name, symbol))
        {
            AddError(typeDecl.Line, typeDecl.Column, $"Failed to define type '{typeDecl.Name}'");
        }
    }

    private void ProcessRoutineDeclaration(RoutineDeclarationNode routineDecl)
    {
        AnnotateScope(routineDecl);

        bool isFullDecl = IsFullDeclaration(routineDecl);
        bool isForwardDecl = !isFullDecl;

        // Check if this routine name is already defined
        if (_symbolTable.IsDefinedLocally(routineDecl.Name))
        {
            // Check if it's a forward declaration that we're now implementing
            if (_forwardDeclarations.ContainsKey(routineDecl.Name) && isFullDecl)
            {
                // This is the full implementation of a forward declaration
                var forwardDecl = _forwardDeclarations[routineDecl.Name];
                
                // Check if signatures match
                if (!SignaturesMatch(forwardDecl, routineDecl))
                {
                    AddError(routineDecl.Line, routineDecl.Column, 
                        $"Signature of routine '{routineDecl.Name}' does not match forward declaration");
                    return;
                }
                
                // Remove from forward declarations as it's now fully defined
                _forwardDeclarations.Remove(routineDecl.Name);
                
                // Update the symbol's declaration node to point to the full implementation
                var existingSymbol = _symbolTable.LookupLocal(routineDecl.Name);
                if (existingSymbol != null)
                {
                    existingSymbol.DeclarationNode = routineDecl;
                }
                
                return; // Don't add a new symbol, we already have one from the forward declaration
            }
            else
            {
                // Both are forward declarations, or both are full declarations - error
                AddError(routineDecl.Line, routineDecl.Column, $"Routine '{routineDecl.Name}' is already defined in this scope");
                return;
            }
        }

        Type? returnType = null;
        if (routineDecl.ReturnType != null)
        {
            returnType = ResolveTypeNode(routineDecl.ReturnType);
            if (returnType == null)
            {
                return;
            }
        }

        var symbol = new Symbol(routineDecl.Name, SymbolKind.Routine, returnType)
        {
            DeclarationNode = routineDecl,
            Scope = _symbolTable.CurrentScope
        };
        symbol.Attributes["Parameters"] = routineDecl.Parameters;
        symbol.Attributes["ReturnType"] = routineDecl.ReturnType;

        symbol.CodeGenInfo!.IsGlobal = _symbolTable.CurrentLevel == 0;
        symbol.CodeGenInfo!.IsLocal = _symbolTable.CurrentLevel > 0;

        if (!_symbolTable.Enter(routineDecl.Name, symbol))
        {
            AddError(routineDecl.Line, routineDecl.Column, $"Failed to define routine '{routineDecl.Name}'");
            return;
        }

        if (isForwardDecl)
        {
            _forwardDeclarations[routineDecl.Name] = routineDecl;
        }
    }

    private void ProcessParameter(ParameterNode parameter, int parameterIndex)
    {
        AnnotateScope(parameter);

        if (_symbolTable.IsDefinedLocally(parameter.Name))
        {
            AddError(parameter.Line, parameter.Column, $"Parameter '{parameter.Name}' is already defined");
            return;
        }

        var type = ResolveTypeNode(parameter.Type);
        if (type == null)
        {
            return;
        }

        var symbol = new Symbol(parameter.Name, SymbolKind.Parameter, type)
        {
            DeclarationNode = parameter,
            Scope = _symbolTable.CurrentScope
        };

        symbol.CodeGenInfo!.IsParameter = true;
        symbol.CodeGenInfo!.ParameterIndex = parameterIndex;

        if (!_symbolTable.Enter(parameter.Name, symbol))
        {
            AddError(parameter.Line, parameter.Column, $"Failed to define parameter '{parameter.Name}'");
        }
    }


    private Type? ResolveTypeNode(TypeNode typeNode)
    {
        return typeNode switch
        {
            PrimitiveTypeNode prim => ResolvePrimitiveType(prim),
            UserTypeNode user => ResolveUserType(user),
            ArrayTypeNode arr => ResolveArrayType(arr),
            RecordTypeNode rec => ResolveRecordType(rec),
            _ => null
        };
    }

    private Type? ResolvePrimitiveType(PrimitiveTypeNode prim)
    {
        var symbol = _symbolTable.Lookup(prim.TypeName);
        if (symbol == null || symbol.Kind != SymbolKind.Type)
        {
            AddError(prim.Line, prim.Column, $"Unknown primitive type '{prim.TypeName}'");
            return null;
        }

        return symbol.Type;
    }

    private Type? ResolveUserType(UserTypeNode user)
    {
        if (_typeResolutionStack.Contains(user.TypeName))
        {
            AddError(user.Line, user.Column,
                $"Circular type reference detected: '{user.TypeName}'");
            return null;
        }

        var symbol = _symbolTable.Lookup(user.TypeName);
        if (symbol == null || symbol.Kind != SymbolKind.Type)
        {
            AddError(user.Line, user.Column, $"Unknown type '{user.TypeName}'");
            return null;
        }

        return symbol.Type;
    }

    private Type? ResolveArrayType(ArrayTypeNode arr)
    {
        var elementType = ResolveTypeNode(arr.ElementType);
        if (elementType == null)
        {
            var typeName = arr.ElementType switch
            {
                PrimitiveTypeNode p => p.TypeName,
                UserTypeNode u => u.TypeName,
                _ => "unknown"
            };
            AddError(arr.ElementType.Line, arr.ElementType.Column,
                $"Unknown element type '{typeName}' in array declaration");
            return null;
        }

        var size = EvaluateConstantInteger(arr.Size);
        if (size == null)
        {
            AddError(arr.Size.Line, arr.Size.Column,
                "Array size must be a constant integer expression");
            return new ArrayType(elementType, 0);
        }

        if (size.Value <= 0)
        {
            AddError(arr.Size.Line, arr.Size.Column,
                "Array size must be positive");
            return new ArrayType(elementType, 0);
        }

        return new ArrayType(elementType, (int)size.Value);
    }

    private Type? ResolveRecordType(RecordTypeNode rec)
    {
        var fields = new Dictionary<string, Type>();

        foreach (var field in rec.Fields)
        {
            if (fields.ContainsKey(field.Name))
            {
                AddError(field.Line, field.Column, $"Duplicate field name '{field.Name}' in record type");
                continue;
            }

            var fieldType = ResolveTypeNode(field.Type);
            if (fieldType == null)
            {
                var typeName = field.Type switch
                {
                    PrimitiveTypeNode p => p.TypeName,
                    UserTypeNode u => u.TypeName,
                    _ => "unknown"
                };
                AddError(field.Type.Line, field.Type.Column,
                    $"Unknown field type '{typeName}' in record declaration");
                continue;
            }

            fields[field.Name] = fieldType;
        }

        return fields.Count > 0 ? new RecordType(fields) : null;
    }


    private void Pass2_TypeChecking(ProgramNode program)
    {
        program.Scope = _symbolTable.GlobalScope;
        program.ScopeLevel = _symbolTable.CurrentLevel;

        foreach (var declaration in program.Declarations)
        {
            CheckDeclaration(declaration);
        }

        ComputeGlobalVariableAddresses(program);
    }

    private void ComputeGlobalVariableAddresses(ProgramNode program)
    {
        int globalAddress = 0;
        foreach (var declaration in program.Declarations)
        {
            if (declaration is VariableDeclarationNode varDecl)
            {
                var symbol = _symbolTable.LookupLocal(varDecl.Name);
                if (symbol != null && symbol.CodeGenInfo != null && symbol.CodeGenInfo.IsGlobal)
                {
                    symbol.CodeGenInfo.Address = globalAddress;
                    var type = ResolveTypeNode(varDecl.Type);
                    if (type != null)
                    {
                        globalAddress += ComputeTypeSize(type);
                    }
                }
            }
        }
    }

    private void AnnotateScope(AstNode node)
    {
        node.Scope = _symbolTable.CurrentScope;
        node.ScopeLevel = _symbolTable.CurrentLevel;
    }

    private void CheckDeclaration(DeclarationNode declaration)
    {
        switch (declaration)
        {
            case VariableDeclarationNode varDecl:
                CheckVariableDeclaration(varDecl);
                break;

            case RoutineDeclarationNode routineDecl:
                CheckRoutineDeclaration(routineDecl);
                break;
        }
    }

    private void CheckVariableDeclaration(VariableDeclarationNode varDecl)
    {
        AnnotateScope(varDecl);

        if (_symbolTable.IsDefinedLocally(varDecl.Name))
        {
            AddError(varDecl.Line, varDecl.Column, $"Variable '{varDecl.Name}' is already defined in this scope");
            return;
        }

        Type? varType;

        if (varDecl.Type == null)
        {
            if (varDecl.InitialValue == null)
            {
                AddError(varDecl.Line, varDecl.Column, $"Variable '{varDecl.Name}' must have an initial value when type is not specified");
                return;
            }

            varType = DeriveType(varDecl.InitialValue);
            if (varType == null)
            {
                AddError(varDecl.InitialValue.Line, varDecl.InitialValue.Column,
                    $"Cannot infer type for variable '{varDecl.Name}' from expression");
                return;
            }

            varDecl.Type = TypeNodeFactory.CreateFromType(varType, varDecl.InitialValue, _symbolTable);
        }
        else
        {
            varType = ResolveTypeNode(varDecl.Type);
            if (varType == null)
            {
                return;
            }

            if (varDecl.InitialValue != null)
            {
                var valueType = DeriveType(varDecl.InitialValue);
                if (valueType != null && !valueType.IsCompatibleWith(varType))
                {
                    AddError(varDecl.InitialValue.Line, varDecl.InitialValue.Column,
                        $"Type mismatch: cannot assign {valueType.Name} to {varType.Name}");
                }
            }
        }

        var symbol = new Symbol(varDecl.Name, SymbolKind.Variable, varType)
        {
            DeclarationNode = varDecl,
            Scope = _symbolTable.CurrentScope
        };

        symbol.CodeGenInfo!.IsGlobal = _symbolTable.CurrentLevel == 0;
        symbol.CodeGenInfo!.IsLocal = _symbolTable.CurrentLevel > 0;

        if (!_symbolTable.Enter(varDecl.Name, symbol))
        {
            AddError(varDecl.Line, varDecl.Column, $"Failed to define variable '{varDecl.Name}'");
            return;
        }

        varDecl.CodeGenInfo = symbol.CodeGenInfo;
    }

    private void CheckRoutineDeclaration(RoutineDeclarationNode routineDecl)
    {
        AnnotateScope(routineDecl);

        _currentRoutine = routineDecl;
        _symbolTable.PushScope(routineDecl.Name);

        int parameterOffset = 0;
        for (int i = 0; i < routineDecl.Parameters.Count; i++)
        {
            var parameter = routineDecl.Parameters[i];
            AnnotateScope(parameter);

            var type = ResolveTypeNode(parameter.Type);
            if (type != null)
            {
                var paramSymbol = new Symbol(parameter.Name, SymbolKind.Parameter, type)
                {
                    DeclarationNode = parameter,
                    Scope = _symbolTable.CurrentScope
                };
                paramSymbol.CodeGenInfo!.IsParameter = true;
                paramSymbol.CodeGenInfo!.ParameterIndex = i;
                paramSymbol.CodeGenInfo!.Offset = parameterOffset;
                parameterOffset += ComputeTypeSize(type);
                _symbolTable.Enter(parameter.Name, paramSymbol);
            }
        }

        if (routineDecl.Body != null)
        {
            CheckBody(routineDecl.Body);
            ComputeLocalVariableOffsets(routineDecl.Body);
            
            if (routineDecl.ReturnType != null)
            {
                CheckAllPathsReturn(routineDecl.Body, routineDecl.Name);
            }
        }

        _symbolTable.PopScope();
        _currentRoutine = null;
    }

    private void ComputeLocalVariableOffsets(BodyNode body)
    {
        int localOffset = 0;
        foreach (var declaration in body.Declarations)
        {
            if (declaration is VariableDeclarationNode varDecl)
            {
                var symbol = _symbolTable.LookupLocal(varDecl.Name);
                if (symbol != null && symbol.CodeGenInfo != null)
                {
                    symbol.CodeGenInfo.Offset = localOffset;
                    symbol.CodeGenInfo.FrameOffset = localOffset;
                    var type = ResolveTypeNode(varDecl.Type);
                    if (type != null)
                    {
                        localOffset += ComputeTypeSize(type);
                    }
                }
            }
        }
    }

    private int ComputeTypeSize(Type type)
    {
        return type switch
        {
            PrimitiveType prim => prim.Kind switch
            {
                PrimitiveKind.Integer => 4,
                PrimitiveKind.Real => 8,
                PrimitiveKind.Boolean => 1,
                _ => 4
            },
            ArrayType arr => arr.Size * ComputeTypeSize(arr.ElementType),
            RecordType rec => rec.Fields.Values.Sum(fieldType => ComputeTypeSize(fieldType)),
            _ => 4
        };
    }

    private void CheckBody(BodyNode body)
    {
        AnnotateScope(body);

        foreach (var declaration in body.Declarations)
        {
            CheckDeclaration(declaration);
        }

        foreach (var statement in body.Statements)
        {
            CheckStatement(statement);
        }
    }

    private void CheckStatement(StatementNode statement)
    {
        AnnotateScope(statement);

        switch (statement)
        {
            case AssignmentNode assign:
                CheckAssignment(assign);
                break;

            case IfStatementNode ifStmt:
                CheckIfStatement(ifStmt);
                break;

            case WhileLoopNode whileLoop:
                CheckWhileLoop(whileLoop);
                break;

            case ForLoopNode forLoop:
                CheckForLoop(forLoop);
                break;

            case ReturnStatementNode returnStmt:
                CheckReturnStatement(returnStmt);
                break;

            case PrintStatementNode printStmt:
                CheckPrintStatement(printStmt);
                break;
        }
    }

    private void CheckAssignment(AssignmentNode assign)
    {
        if (!IsModifiable(assign.Target))
        {
            AddError(assign.Target.Line, assign.Target.Column, "Assignment target must be modifiable");
            return;
        }

        var targetType = DeriveType(assign.Target);
        var valueType = DeriveType(assign.Value);
        if (targetType != null && valueType != null && !valueType.IsCompatibleWith(targetType))
            AddError(assign.Line, assign.Column, $"Type mismatch: cannot assign {valueType.Name} to {targetType.Name}");
    }

    private void CheckBooleanCondition(ExpressionNode condition, int line, int column)
    {
        var conditionType = DeriveType(condition);
        if (conditionType != null && !AreCompatible(conditionType, BooleanType) &&
            !(conditionType is PrimitiveType { Kind: PrimitiveKind.Integer }))
            AddError(line, column, $"Condition must be boolean, got {conditionType.Name}");
    }

    private void CheckIfStatement(IfStatementNode ifStmt)
    {
        CheckBooleanCondition(ifStmt.Condition, ifStmt.Condition.Line, ifStmt.Condition.Column);
        foreach (var stmt in ifStmt.ThenBody) CheckStatement(stmt);
        foreach (var stmt in ifStmt.ElseBody) CheckStatement(stmt);
    }

    private void CheckWhileLoop(WhileLoopNode whileLoop)
    {
        CheckBooleanCondition(whileLoop.Condition, whileLoop.Condition.Line, whileLoop.Condition.Column);
        foreach (var stmt in whileLoop.Body) CheckStatement(stmt);
    }

    private void CheckForLoop(ForLoopNode forLoop)
    {
        AnnotateScope(forLoop);

        var rangeType = DeriveType(forLoop.Range);
        if (rangeType == null)
        {
            return;
        }

        if (!(rangeType is RecordType rangeRecord) || rangeRecord.Fields.Count != 2)
        {
            AddError(forLoop.Range.Line, forLoop.Range.Column,
                "For loop range must be a range expression");
            return;
        }

        _symbolTable.PushScope("for_loop");
        var loopVarSymbol = new Symbol(forLoop.Variable, SymbolKind.Variable, IntegerType)
        {
            DeclarationNode = forLoop,
            Scope = _symbolTable.CurrentScope
        };
        loopVarSymbol.CodeGenInfo!.IsLocal = true;
        _symbolTable.Enter(forLoop.Variable, loopVarSymbol);

        foreach (var stmt in forLoop.Body)
        {
            CheckStatement(stmt);
        }

        _symbolTable.PopScope();
    }

    private void CheckReturnStatement(ReturnStatementNode returnStmt)
    {
        if (_currentRoutine == null)
        {
            AddError(returnStmt.Line, returnStmt.Column, "Return statement must be inside a routine");
            return;
        }

        var hasValue = returnStmt.Value != null;
        var expectsValue = _currentRoutine.ReturnType != null;

        if (hasValue != expectsValue)
        {
            if (expectsValue)
                AddError(returnStmt.Line, returnStmt.Column, $"Routine '{_currentRoutine.Name}' expects a return value, but none provided");
            else
                AddError(returnStmt.Line, returnStmt.Column, $"Routine '{_currentRoutine.Name}' does not return a value, but return statement provides one");
            return;
        }

        if (hasValue && expectsValue)
        {
            var returnType = DeriveType(returnStmt.Value!);
            var expectedType = ResolveTypeNode(_currentRoutine.ReturnType!);
            if (returnType != null && expectedType != null && !returnType.IsCompatibleWith(expectedType))
                AddError(returnStmt.Value!.Line, returnStmt.Value!.Column,
                    $"Return type mismatch in routine '{_currentRoutine.Name}': expected {expectedType.Name}, got {returnType.Name}");
        }
    }

    private void CheckPrintStatement(PrintStatementNode printStmt)
    {
        DeriveType(printStmt.Expression);
    }

    public Type? DeriveType(ExpressionNode expression)
    {
        return CheckExpression(expression);
    }

    private Type? CheckExpression(ExpressionNode expression)
    {
        AnnotateScope(expression);

        Type? type = expression switch
        {
            IntegerLiteralNode => IntegerType,
            RealLiteralNode => RealType,
            BooleanLiteralNode => BooleanType,
            IdentifierNode id => CheckIdentifier(id),
            BinaryOperationNode binOp => CheckBinaryOperation(binOp),
            UnaryOperationNode unOp => CheckUnaryOperation(unOp),
            ArrayAccessNode arrAccess => CheckArrayAccess(arrAccess),
            RecordAccessNode recAccess => CheckRecordAccess(recAccess),
            RoutineCallNode call => CheckRoutineCall(call),
            RangeNode range => CheckRange(range),
            ArrayInitializerNode arrInit => CheckArrayInitializer(arrInit),
            _ => null
        };

        if (type != null)
        {
            expression.Type = type;
        }

        return type;
    }

    private Type? CheckIdentifier(IdentifierNode id)
    {
        var symbol = _symbolTable.Lookup(id.Name);
        if (symbol == null)
        {
            AddError(id.Line, id.Column, $"Undeclared variable '{id.Name}'");
            return null;
        }

        id.Symbol = symbol;
        id.Type = symbol.Type;
        id.CodeGenInfo = symbol.CodeGenInfo;
        return symbol.Type;
    }

    private Type? CheckBinaryOperation(BinaryOperationNode binOp)
    {
        var leftType = DeriveType(binOp.Left);
        var rightType = DeriveType(binOp.Right);

        if (leftType == null || rightType == null)
        {
            return null;
        }

        var resultType = binOp.Operator switch
        {
            "+" or "-" or "*" or "/" => CheckArithmeticOperation(binOp, leftType, rightType),
            "=" or "<>" => CheckComparisonOperation(binOp, leftType, rightType),
            "<" or ">" or "<=" or ">=" => CheckRelationalOperation(binOp, leftType, rightType),
            "and" or "or" => CheckLogicalOperation(binOp, leftType, rightType),
            _ => null
        };

        if (resultType != null)
        {
            binOp.Type = resultType;
        }

        return resultType;
    }

    private static bool IsNumeric(PrimitiveType prim) => prim.Kind == PrimitiveKind.Integer || prim.Kind == PrimitiveKind.Real;

    private Type? CheckArithmeticOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        if (leftType is PrimitiveType leftPrim && rightType is PrimitiveType rightPrim)
        {
            if (leftPrim.Kind == PrimitiveKind.Integer && rightPrim.Kind == PrimitiveKind.Integer)
            {
                binOp.Type = IntegerType;
                return IntegerType;
            }
            if (IsNumeric(leftPrim) && IsNumeric(rightPrim))
            {
                binOp.Type = RealType;
                return RealType;
            }
        }

        var commonType = PrimitiveType.GetCommonType(leftType, rightType);
        if (commonType is PrimitiveType commonPrim && IsNumeric(commonPrim))
        {
            binOp.Type = commonPrim;
            return commonPrim;
        }

        AddError(binOp.Line, binOp.Column, $"Invalid operands for '{binOp.Operator}': {leftType.Name} and {rightType.Name}");
        return null;
    }

    private Type? CheckComparisonOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        if (AreCompatible(leftType, rightType) || PrimitiveType.GetCommonType(leftType, rightType) != null)
        {
            binOp.Type = BooleanType;
            return BooleanType;
        }
        AddError(binOp.Line, binOp.Column, $"Type mismatch in '{binOp.Operator}': {leftType.Name} and {rightType.Name} are not compatible");
        return null;
    }

    private Type? CheckRelationalOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        if (leftType is PrimitiveType leftPrim && rightType is PrimitiveType rightPrim && IsNumeric(leftPrim) && IsNumeric(rightPrim))
        {
            binOp.Type = BooleanType;
            return BooleanType;
        }

        var commonType = PrimitiveType.GetCommonType(leftType, rightType);
        if (commonType is PrimitiveType commonPrim && IsNumeric(commonPrim))
        {
            binOp.Type = BooleanType;
            return BooleanType;
        }

        AddError(binOp.Line, binOp.Column, $"Invalid operands for '{binOp.Operator}': {leftType.Name} and {rightType.Name}");
        return null;
    }

    private Type? CheckLogicalOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        if ((AreCompatible(leftType, BooleanType) && AreCompatible(rightType, BooleanType)) ||
            (leftType is PrimitiveType leftPrim && rightType is PrimitiveType rightPrim &&
             (leftPrim.Kind == PrimitiveKind.Boolean || leftPrim.Kind == PrimitiveKind.Integer) &&
             (rightPrim.Kind == PrimitiveKind.Boolean || rightPrim.Kind == PrimitiveKind.Integer)))
        {
            binOp.Type = BooleanType;
            return BooleanType;
        }

        AddError(binOp.Line, binOp.Column, $"Logical operation '{binOp.Operator}' requires boolean or integer operands");
        return null;
    }

    private Type? CheckUnaryOperation(UnaryOperationNode unOp)
    {
        var operandType = DeriveType(unOp.Operand);
        if (operandType == null) return null;

        var resultType = unOp.Operator switch
        {
            "-" => operandType is PrimitiveType p && IsNumeric(p) ? operandType : null,
            "not" => AreCompatible(operandType, BooleanType) || operandType is PrimitiveType { Kind: PrimitiveKind.Integer } 
                ? BooleanType : null,
            _ => null
        };

        if (resultType == null)
            AddError(unOp.Line, unOp.Column, $"Invalid unary operation '{unOp.Operator}'");
        else
            unOp.Type = resultType;
        return resultType;
    }

    private Type? CheckArrayAccess(ArrayAccessNode arrAccess)
    {
        var arrayType = DeriveType(arrAccess.Array);
        if (arrayType == null)
        {
            return null;
        }

        if (!(arrayType is ArrayType arrType))
        {
            AddError(arrAccess.Array.Line, arrAccess.Array.Column,
                $"Cannot index non-array type {arrayType.Name}");
            return null;
        }

        var indexType = DeriveType(arrAccess.Index);
        if (indexType == null)
        {
            return null;
        }

        if (!AreCompatible(indexType, IntegerType))
        {
            var commonType = PrimitiveType.GetCommonType(indexType, IntegerType);
            if (commonType is not PrimitiveType { Kind: PrimitiveKind.Integer })
            {
                AddError(arrAccess.Index.Line, arrAccess.Index.Column,
                    $"Array index must be integer, got {indexType.Name}");
                arrAccess.Type = arrType.ElementType;
                return arrType.ElementType;
            }
        }

        var indexValue = EvaluateConstantInteger(arrAccess.Index);
        if (indexValue != null)
        {
            if (arrType.Size > 0)
            {
                if (indexValue.Value < 1 || indexValue.Value > arrType.Size)
                {
                    AddError(arrAccess.Index.Line, arrAccess.Index.Column,
                        $"Array index {indexValue.Value} is out of bounds. Array size is {arrType.Size}, valid range is 1..{arrType.Size}");
                }
            }
        }
        else
        {
            if (arrType.Size > 0)
            {
                AddError(arrAccess.Index.Line, arrAccess.Index.Column,
                    $"Warning: Array index cannot be statically verified. Array size is {arrType.Size}, ensure index is in range 1..{arrType.Size}");
            }
        }

        arrAccess.Type = arrType.ElementType;
        return arrType.ElementType;
    }

    private Type? CheckRecordAccess(RecordAccessNode recAccess)
    {
        var recordType = DeriveType(recAccess.Record);
        if (recordType == null)
        {
            return null;
        }

        if (!(recordType is RecordType recType))
        {
            AddError(recAccess.Record.Line, recAccess.Record.Column,
                $"Cannot access field of non-record type {recordType.Name}");
            return null;
        }

        if (!recType.Fields.TryGetValue(recAccess.FieldName, out var fieldType))
        {
            AddError(recAccess.Line, recAccess.Column,
                $"Field '{recAccess.FieldName}' does not exist in record type");
            return null;
        }

        recAccess.Type = fieldType;
        return fieldType;
    }

    private Type? CheckRoutineCall(RoutineCallNode call)
    {
        var symbol = _symbolTable.Lookup(call.RoutineName);
        if (symbol == null)
        {
            AddError(call.Line, call.Column, $"Undefined routine '{call.RoutineName}'");
            return null;
        }

        if (symbol.Kind != SymbolKind.Routine)
        {
            AddError(call.Line, call.Column, $"'{call.RoutineName}' is not a routine");
            return null;
        }

        if (symbol.DeclarationNode is RoutineDeclarationNode routineDecl)
        {
            if (!IsFullDeclaration(routineDecl))
            {
                AddError(call.Line, call.Column,
                    $"Routine '{call.RoutineName}' is forward declared but has no full definition");
                call.Type = symbol.Type;
                return symbol.Type;
            }
        }

        var parameters = symbol.Attributes.GetValueOrDefault("Parameters") as List<ParameterNode>;
        if (parameters == null)
        {
            call.Type = symbol.Type;
            return symbol.Type;
        }

        if (call.Arguments.Count != parameters.Count)
        {
            AddError(call.Line, call.Column,
                $"Argument count mismatch: expected {parameters.Count}, got {call.Arguments.Count}");
            call.Type = symbol.Type;
            return symbol.Type;
        }

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            CheckArgumentType(call.Arguments[i], parameters[i], i + 1, call.RoutineName);
        }

        call.Type = symbol.Type;
        return symbol.Type;
    }

    private void CheckArgumentType(ExpressionNode argument, ParameterNode parameter, int argumentIndex, string routineName)
    {
        var argType = DeriveType(argument);
        var paramType = ResolveTypeNode(parameter.Type);
        if (argType != null && paramType != null && !argType.IsCompatibleWith(paramType))
            AddError(argument.Line, argument.Column,
                $"Argument {argumentIndex} type mismatch in call to '{routineName}': expected {paramType.Name}, got {argType.Name}");
    }


    private Type? CheckRange(RangeNode range)
    {
        var startType = DeriveType(range.Start);
        var endType = DeriveType(range.End);

        if (startType == null || endType == null)
        {
            return null;
        }

        if (!AreCompatible(startType, IntegerType) || !AreCompatible(endType, IntegerType))
        {
            AddError(range.Line, range.Column, "Range bounds must be integer");
            return null;
        }

        var fields = new Dictionary<string, Type> { ["start"] = IntegerType, ["end"] = IntegerType };
        var resultType = new RecordType(fields);
        range.Type = resultType;
        return resultType;
    }

    private Type? CheckArrayInitializer(ArrayInitializerNode arrInit)
    {
        if (arrInit.Elements.Count == 0)
        {
            return null;
        }

        var elementType = DeriveType(arrInit.Elements[0]);
        if (elementType == null)
        {
            return null;
        }

        for (int i = 1; i < arrInit.Elements.Count; i++)
        {
            var elemType = DeriveType(arrInit.Elements[i]);
            if (elemType != null && !AreCompatible(elemType, elementType))
                AddError(arrInit.Elements[i].Line, arrInit.Elements[i].Column,
                    $"Array element type mismatch: expected {elementType.Name}, got {elemType.Name}");
        }

        var resultType = new ArrayType(elementType, arrInit.Elements.Count);
        arrInit.Type = resultType;
        return resultType;
    }

    private bool IsModifiable(ExpressionNode expression) => expression switch
    {
        IdentifierNode id => IsVariableOrParameter(id.Name),
        ArrayAccessNode arrAccess => IsModifiable(arrAccess.Array),
        RecordAccessNode recAccess => IsModifiable(recAccess.Record),
        _ => false
    };

    private bool IsVariableOrParameter(string name)
    {
        var symbol = _symbolTable.Lookup(name);
        return symbol?.Kind == SymbolKind.Variable || symbol?.Kind == SymbolKind.Parameter;
    }

    private long? EvaluateConstantInteger(ExpressionNode expression)
    {
        return expression switch
        {
            IntegerLiteralNode intLit => intLit.Value,
            BinaryOperationNode binOp => EvaluateConstantBinaryOperation(binOp),
            UnaryOperationNode unOp => EvaluateConstantUnaryOperation(unOp),
            _ => null
        };
    }

    private long? EvaluateConstantBinaryOperation(BinaryOperationNode binOp)
    {
        var left = EvaluateConstantInteger(binOp.Left);
        var right = EvaluateConstantInteger(binOp.Right);

        if (left == null || right == null)
        {
            return null;
        }

        return binOp.Operator switch
        {
            "+" => left.Value + right.Value,
            "-" => left.Value - right.Value,
            "*" => left.Value * right.Value,
            "/" => right.Value != 0 ? left.Value / right.Value : null,
            _ => null
        };
    }

    private long? EvaluateConstantUnaryOperation(UnaryOperationNode unOp)
    {
        var operand = EvaluateConstantInteger(unOp.Operand);
        if (operand == null)
        {
            return null;
        }

        return unOp.Operator switch
        {
            "-" => -operand.Value,
            _ => null
        };
    }

    private void CheckAllPathsReturn(BodyNode body, string routineName)
    {
        if (!AllPathsReturn(body.Statements))
        {
            int line = body.Line;
            int column = body.Column;
            
            if (body.Statements.Count > 0)
            {
                var lastStatement = body.Statements[body.Statements.Count - 1];
                line = lastStatement.Line;
                column = lastStatement.Column;
            }
            
            AddError(line, column,
                $"Not all execution paths in routine '{routineName}' return a value");
        }
    }

    private bool AllPathsReturn(List<StatementNode> statements, int startIndex = 0)
    {
        if (statements.Count == 0) return false;

        for (int i = startIndex; i < statements.Count; i++)
        {
            if (statements[i] is ReturnStatementNode) return true;

            if (statements[i] is IfStatementNode ifStmt)
            {
                var thenReturns = AllPathsReturn(ifStmt.ThenBody);
                if (ifStmt.ElseBody.Count == 0)
                    return thenReturns && AllPathsReturn(statements, i + 1);
                return thenReturns && AllPathsReturn(ifStmt.ElseBody);
            }

            if (statements[i] is WhileLoopNode or ForLoopNode && i == statements.Count - 1)
                return false;
        }
        return false;
    }

    private void AddError(int line, int column, string message)
    {
        _errors.Add(new SemanticError(line, column, message));
    }
}
