using Compiler.AST;
using System.Collections.Generic;
using System.Linq;

namespace Compiler.Semantic;

public class SemanticAnalyzer
{
    private readonly SymbolTable _symbolTable;
    private readonly List<SemanticError> _errors;
    private RoutineDeclarationNode? _currentRoutine;
    private readonly HashSet<string> _typeResolutionStack;
    private readonly Dictionary<string, RoutineDeclarationNode> _forwardDeclarations;
    
    public IReadOnlyList<SemanticError> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;
    public SymbolTable SymbolTable => _symbolTable;
    
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
        Pass1_TypeDeclarations(program);
        Pass1_RoutineDeclarations(program);
        Pass1_CheckForwardDeclarations(program);
    }
    
    private void Pass1_TypeDeclarations(ProgramNode program)
    {
        foreach (var declaration in program.Declarations)
        {
            if (declaration is TypeDeclarationNode typeDecl)
            {
                ProcessTypeDeclaration(typeDecl);
            }
        }
    }
    
    private void Pass1_RoutineDeclarations(ProgramNode program)
    {
        foreach (var declaration in program.Declarations)
        {
            if (declaration is RoutineDeclarationNode routineDecl)
            {
                ProcessRoutineDeclaration(routineDecl);
            }
        }
    }
    
    private void Pass1_CheckForwardDeclarations(ProgramNode program)
    {
        foreach (var forwardName in _forwardDeclarations.Keys)
        {
            var forwardDecl = _forwardDeclarations[forwardName];
            var fullDecl = program.Declarations
                .OfType<RoutineDeclarationNode>()
                .FirstOrDefault(r => r.Name == forwardName && IsFullDeclaration(r));
            
            if (fullDecl == null)
            {
                AddError(forwardDecl.Line, forwardDecl.Column, 
                    $"Forward declaration of routine '{forwardName}' has no full definition");
                continue;
            }
            
            if (!SignaturesMatch(forwardDecl, fullDecl))
            {
                AddError(fullDecl.Line, fullDecl.Column, 
                    $"Signature of routine '{forwardName}' does not match forward declaration");
            }
        }
    }
    
    private bool IsFullDeclaration(RoutineDeclarationNode routine)
    {
        return routine.Body != null && 
               (routine.Body.Statements.Count > 0 || routine.Body.Declarations.Count > 0);
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
            DeclarationNode = typeDecl
        };
        
        if (!_symbolTable.Enter(typeDecl.Name, symbol))
        {
            AddError(typeDecl.Line, typeDecl.Column, $"Failed to define type '{typeDecl.Name}'");
        }
    }
    
    private void ProcessVariableDeclaration(VariableDeclarationNode varDecl)
    {
        if (_symbolTable.IsDefinedLocally(varDecl.Name))
        {
            AddError(varDecl.Line, varDecl.Column, $"Variable '{varDecl.Name}' is already defined in this scope");
            return;
        }
        
        var type = ResolveTypeNode(varDecl.Type);
        if (type == null)
        {
            return;
        }
        
        var symbol = new Symbol(varDecl.Name, SymbolKind.Variable, type)
        {
            DeclarationNode = varDecl
        };
        
        if (!_symbolTable.Enter(varDecl.Name, symbol))
        {
            AddError(varDecl.Line, varDecl.Column, $"Failed to define variable '{varDecl.Name}'");
        }
    }
    
    private void ProcessRoutineDeclaration(RoutineDeclarationNode routineDecl)
    {
        if (_symbolTable.IsDefinedLocally(routineDecl.Name))
        {
            AddError(routineDecl.Line, routineDecl.Column, $"Routine '{routineDecl.Name}' is already defined in this scope");
            return;
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
            DeclarationNode = routineDecl
        };
        symbol.Attributes["Parameters"] = routineDecl.Parameters;
        symbol.Attributes["ReturnType"] = routineDecl.ReturnType;
        
        if (!_symbolTable.Enter(routineDecl.Name, symbol))
        {
            AddError(routineDecl.Line, routineDecl.Column, $"Failed to define routine '{routineDecl.Name}'");
            return;
        }
        
        if (!IsFullDeclaration(routineDecl))
        {
            _forwardDeclarations[routineDecl.Name] = routineDecl;
        }
    }
    
    private void ProcessParameter(ParameterNode parameter)
    {
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
            DeclarationNode = parameter
        };
        
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
        
        return new ArrayType(elementType, 0);
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
        foreach (var declaration in program.Declarations)
        {
            CheckDeclaration(declaration);
        }
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
        if (_symbolTable.IsDefinedLocally(varDecl.Name))
        {
            AddError(varDecl.Line, varDecl.Column, $"Variable '{varDecl.Name}' is already defined in this scope");
            return;
        }
        
        var varType = ResolveTypeNode(varDecl.Type);
        if (varType == null)
        {
            return;
        }
        
        var symbol = new Symbol(varDecl.Name, SymbolKind.Variable, varType)
        {
            DeclarationNode = varDecl
        };
        
        if (!_symbolTable.Enter(varDecl.Name, symbol))
        {
            AddError(varDecl.Line, varDecl.Column, $"Failed to define variable '{varDecl.Name}'");
            return;
        }
        
        if (varDecl.InitialValue != null)
        {
            var valueType = DeriveType(varDecl.InitialValue);
            
            if (valueType != null && !valueType.IsCompatibleWith(varType) && !varType.IsCompatibleWith(valueType))
            {
                var commonType = PrimitiveType.GetCommonType(valueType, varType);
                if (commonType == null)
                {
                    AddError(varDecl.InitialValue.Line, varDecl.InitialValue.Column, 
                        $"Type mismatch: cannot assign {valueType.Name} to {varType.Name}");
                }
            }
        }
    }
    
    private void CheckRoutineDeclaration(RoutineDeclarationNode routineDecl)
    {
        _currentRoutine = routineDecl;
        _symbolTable.PushScope(routineDecl.Name);
        
        foreach (var parameter in routineDecl.Parameters)
        {
            var type = ResolveTypeNode(parameter.Type);
            if (type != null)
            {
                var paramSymbol = new Symbol(parameter.Name, SymbolKind.Parameter, type)
                {
                    DeclarationNode = parameter
                };
                _symbolTable.Enter(parameter.Name, paramSymbol);
            }
        }
        
        CheckBody(routineDecl.Body);
        
        _symbolTable.PopScope();
        _currentRoutine = null;
    }
    
    private void CheckBody(BodyNode body)
    {
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
        var targetType = DeriveType(assign.Target);
        var valueType = DeriveType(assign.Value);
        
        if (targetType == null || valueType == null)
        {
            return;
        }
        
        if (valueType.IsCompatibleWith(targetType))
        {
            return;
        }
        
        var commonType = PrimitiveType.GetCommonType(valueType, targetType);
        if (commonType != null && commonType.IsCompatibleWith(targetType))
        {
            return;
        }
        
        AddError(assign.Line, assign.Column, 
            $"Type mismatch in assignment: cannot assign {valueType.Name} to {targetType.Name}");
    }
    
    private void CheckIfStatement(IfStatementNode ifStmt)
    {
        var conditionType = DeriveType(ifStmt.Condition);
        if (conditionType != null)
        {
            var boolType = new PrimitiveType(PrimitiveKind.Boolean);
            
            if (conditionType.IsCompatibleWith(boolType) || boolType.IsCompatibleWith(conditionType))
            {
                return;
            }
            
            if (conditionType is PrimitiveType prim && prim.Kind == PrimitiveKind.Integer)
            {
                return;
            }
            
            var commonType = PrimitiveType.GetCommonType(conditionType, boolType);
            if (commonType != null)
            {
                return;
            }
            
            AddError(ifStmt.Condition.Line, ifStmt.Condition.Column, 
                $"Condition must be boolean, got {conditionType.Name}");
        }
        
        foreach (var stmt in ifStmt.ThenBody)
        {
            CheckStatement(stmt);
        }
        
        foreach (var stmt in ifStmt.ElseBody)
        {
            CheckStatement(stmt);
        }
    }
    
    private void CheckWhileLoop(WhileLoopNode whileLoop)
    {
        var conditionType = DeriveType(whileLoop.Condition);
        if (conditionType != null)
        {
            var boolType = new PrimitiveType(PrimitiveKind.Boolean);
            
            if (conditionType.IsCompatibleWith(boolType) || boolType.IsCompatibleWith(conditionType))
            {
                return;
            }
            
            if (conditionType is PrimitiveType prim && prim.Kind == PrimitiveKind.Integer)
            {
                return;
            }
            
            var commonType = PrimitiveType.GetCommonType(conditionType, boolType);
            if (commonType != null)
            {
                return;
            }
            
            AddError(whileLoop.Condition.Line, whileLoop.Condition.Column, 
                $"Condition must be boolean, got {conditionType.Name}");
        }
        
        foreach (var stmt in whileLoop.Body)
        {
            CheckStatement(stmt);
        }
    }
    
    private void CheckForLoop(ForLoopNode forLoop)
    {
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
        
        var loopVarSymbol = new Symbol(forLoop.Variable, SymbolKind.Variable, 
            new PrimitiveType(PrimitiveKind.Integer))
        {
            DeclarationNode = forLoop
        };
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
            AddError(returnStmt.Line, returnStmt.Column, "Return statement outside of routine");
            return;
        }
        
        if (returnStmt.Value == null)
        {
            if (_currentRoutine.ReturnType != null)
            {
                AddError(returnStmt.Line, returnStmt.Column, 
                    $"Routine '{_currentRoutine.Name}' expects a return value");
            }
        }
        else
        {
            if (_currentRoutine.ReturnType == null)
            {
                AddError(returnStmt.Line, returnStmt.Column, 
                    $"Routine '{_currentRoutine.Name}' does not return a value");
            }
            else
            {
                var returnType = DeriveType(returnStmt.Value);
                var expectedType = ResolveTypeNode(_currentRoutine.ReturnType);
                
                if (returnType == null || expectedType == null)
                {
                    return;
                }
                
                if (returnType.IsCompatibleWith(expectedType))
                {
                    return;
                }
                
                var commonType = PrimitiveType.GetCommonType(returnType, expectedType);
                if (commonType != null && commonType.IsCompatibleWith(expectedType))
                {
                    return;
                }
                
                AddError(returnStmt.Value.Line, returnStmt.Value.Column, 
                    $"Return type mismatch: expected {expectedType.Name}, got {returnType.Name}");
            }
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
        return expression switch
        {
            IntegerLiteralNode => new PrimitiveType(PrimitiveKind.Integer),
            RealLiteralNode => new PrimitiveType(PrimitiveKind.Real),
            BooleanLiteralNode => new PrimitiveType(PrimitiveKind.Boolean),
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
    }
    
    private Type? CheckIdentifier(IdentifierNode id)
    {
        var symbol = _symbolTable.Lookup(id.Name);
        if (symbol == null)
        {
            AddError(id.Line, id.Column, $"Undeclared variable '{id.Name}'");
            return null;
        }
        
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
        
        return binOp.Operator switch
        {
            "+" or "-" or "*" or "/" => CheckArithmeticOperation(binOp, leftType, rightType),
            "=" or "<>" => CheckComparisonOperation(binOp, leftType, rightType),
            "<" or ">" or "<=" or ">=" => CheckRelationalOperation(binOp, leftType, rightType),
            "and" or "or" => CheckLogicalOperation(binOp, leftType, rightType),
            _ => null
        };
    }
    
    private Type? CheckArithmeticOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        if (leftType is PrimitiveType leftPrim && rightType is PrimitiveType rightPrim)
        {
            if (leftPrim.Kind == PrimitiveKind.Integer && rightPrim.Kind == PrimitiveKind.Integer)
            {
                return new PrimitiveType(PrimitiveKind.Integer);
            }
            
            if ((leftPrim.Kind == PrimitiveKind.Integer || leftPrim.Kind == PrimitiveKind.Real) &&
                (rightPrim.Kind == PrimitiveKind.Integer || rightPrim.Kind == PrimitiveKind.Real))
            {
                return new PrimitiveType(PrimitiveKind.Real);
            }
        }
        
        var commonType = PrimitiveType.GetCommonType(leftType, rightType);
        if (commonType != null && commonType is PrimitiveType commonPrim)
        {
            if (commonPrim.Kind == PrimitiveKind.Integer || commonPrim.Kind == PrimitiveKind.Real)
            {
                return commonPrim;
            }
        }
        
        AddError(binOp.Line, binOp.Column, 
            $"Invalid operands for arithmetic operation '{binOp.Operator}': {leftType.Name} and {rightType.Name}");
        return null;
    }
    
    private Type? CheckComparisonOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        if (leftType.IsCompatibleWith(rightType) || rightType.IsCompatibleWith(leftType))
        {
            return new PrimitiveType(PrimitiveKind.Boolean);
        }
        
        var commonType = PrimitiveType.GetCommonType(leftType, rightType);
        if (commonType != null)
        {
            return new PrimitiveType(PrimitiveKind.Boolean);
        }
        
        AddError(binOp.Line, binOp.Column, 
            $"Type mismatch in comparison '{binOp.Operator}': {leftType.Name} and {rightType.Name} are not compatible");
        return null;
    }
    
    private Type? CheckRelationalOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        if (leftType is PrimitiveType leftPrim && rightType is PrimitiveType rightPrim)
        {
            if ((leftPrim.Kind == PrimitiveKind.Integer || leftPrim.Kind == PrimitiveKind.Real) &&
                (rightPrim.Kind == PrimitiveKind.Integer || rightPrim.Kind == PrimitiveKind.Real))
            {
                return new PrimitiveType(PrimitiveKind.Boolean);
            }
        }
        
        var commonType = PrimitiveType.GetCommonType(leftType, rightType);
        if (commonType != null && commonType is PrimitiveType commonPrim)
        {
            if (commonPrim.Kind == PrimitiveKind.Integer || commonPrim.Kind == PrimitiveKind.Real)
            {
                return new PrimitiveType(PrimitiveKind.Boolean);
            }
        }
        
        AddError(binOp.Line, binOp.Column, 
            $"Invalid operands for relational operation '{binOp.Operator}': {leftType.Name} and {rightType.Name}");
        return null;
    }
    
    private Type? CheckLogicalOperation(BinaryOperationNode binOp, Type leftType, Type rightType)
    {
        var boolType = new PrimitiveType(PrimitiveKind.Boolean);
        if ((leftType.IsCompatibleWith(boolType) || boolType.IsCompatibleWith(leftType)) &&
            (rightType.IsCompatibleWith(boolType) || boolType.IsCompatibleWith(rightType)))
        {
            return boolType;
        }
        
        var commonType = PrimitiveType.GetCommonType(leftType, rightType);
        if (commonType != null)
        {
            if (leftType is PrimitiveType leftPrim && 
                (leftPrim.Kind == PrimitiveKind.Boolean || leftPrim.Kind == PrimitiveKind.Integer))
            {
                if (rightType is PrimitiveType rightPrim &&
                    (rightPrim.Kind == PrimitiveKind.Boolean || rightPrim.Kind == PrimitiveKind.Integer))
                {
                    return boolType;
                }
            }
        }
        
        AddError(binOp.Line, binOp.Column, 
            $"Logical operation '{binOp.Operator}' requires boolean or integer operands, got {leftType.Name} and {rightType.Name}");
        return null;
    }
    
    private Type? CheckUnaryOperation(UnaryOperationNode unOp)
    {
        var operandType = DeriveType(unOp.Operand);
        if (operandType == null)
        {
            return null;
        }
        
        return unOp.Operator switch
        {
            "-" => CheckUnaryMinus(unOp, operandType),
            "not" => CheckUnaryNot(unOp, operandType),
            _ => null
        };
    }
    
    private Type? CheckUnaryMinus(UnaryOperationNode unOp, Type operandType)
    {
        if (operandType is PrimitiveType prim && 
            (prim.Kind == PrimitiveKind.Integer || prim.Kind == PrimitiveKind.Real))
        {
            return operandType;
        }
        
        AddError(unOp.Line, unOp.Column, 
            $"Unary minus requires numeric operand, got {operandType.Name}");
        return null;
    }
    
    private Type? CheckUnaryNot(UnaryOperationNode unOp, Type operandType)
    {
        var boolType = new PrimitiveType(PrimitiveKind.Boolean);
        if (operandType.IsCompatibleWith(boolType) || boolType.IsCompatibleWith(operandType))
        {
            return boolType;
        }
        
        if (operandType is PrimitiveType prim && prim.Kind == PrimitiveKind.Integer)
        {
            return boolType;
        }
        
        AddError(unOp.Line, unOp.Column, 
            $"Unary 'not' requires boolean or integer operand, got {operandType.Name}");
        return null;
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
        
        var intType = new PrimitiveType(PrimitiveKind.Integer);
        if (!indexType.IsCompatibleWith(intType) && !intType.IsCompatibleWith(indexType))
        {
            var commonType = PrimitiveType.GetCommonType(indexType, intType);
            if (commonType == null || !(commonType is PrimitiveType commonPrim && commonPrim.Kind == PrimitiveKind.Integer))
            {
                AddError(arrAccess.Index.Line, arrAccess.Index.Column, 
                    $"Array index must be integer, got {indexType.Name}");
            }
        }
        
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
                return symbol.Type;
            }
        }
        
        var parameters = symbol.Attributes.GetValueOrDefault("Parameters") as List<ParameterNode>;
        if (parameters == null)
        {
            return symbol.Type;
        }
        
        if (call.Arguments.Count != parameters.Count)
        {
            AddError(call.Line, call.Column, 
                $"Argument count mismatch: expected {parameters.Count}, got {call.Arguments.Count}");
            return symbol.Type;
        }
        
        for (int i = 0; i < call.Arguments.Count; i++)
        {
            CheckArgumentType(call.Arguments[i], parameters[i], i + 1, call.RoutineName);
        }
        
        return symbol.Type;
    }
    
    private void CheckArgumentType(ExpressionNode argument, ParameterNode parameter, int argumentIndex, string routineName)
    {
        var argType = DeriveType(argument);
        var paramType = ResolveTypeNode(parameter.Type);
        
        if (argType == null || paramType == null)
        {
            return;
        }
        
        if (argType.IsCompatibleWith(paramType))
        {
            return;
        }
        
        if (CanConvertArgumentToParameterType(argType, paramType))
        {
            return;
        }
        
        AddError(argument.Line, argument.Column, 
            $"Argument {argumentIndex} type mismatch in call to '{routineName}': expected {paramType.Name}, got {argType.Name}");
    }
    
    private bool CanConvertArgumentToParameterType(Type argType, Type paramType)
    {
        if (argType is PrimitiveType argPrim && paramType is PrimitiveType paramPrim)
        {
            if (argPrim.Kind == PrimitiveKind.Integer && paramPrim.Kind == PrimitiveKind.Real)
            {
                return true;
            }
            
            if ((argPrim.Kind == PrimitiveKind.Integer && paramPrim.Kind == PrimitiveKind.Boolean) ||
                (argPrim.Kind == PrimitiveKind.Boolean && paramPrim.Kind == PrimitiveKind.Integer))
            {
                return true;
            }
        }
        
        var commonType = PrimitiveType.GetCommonType(argType, paramType);
        if (commonType != null && commonType.Equals(paramType))
        {
            return true;
        }
        
        return false;
    }
    
    private Type? CheckRange(RangeNode range)
    {
        var startType = DeriveType(range.Start);
        var endType = DeriveType(range.End);
        
        if (startType == null || endType == null)
        {
            return null;
        }
        
        var intType = new PrimitiveType(PrimitiveKind.Integer);
        if ((!startType.IsCompatibleWith(intType) && !intType.IsCompatibleWith(startType)) ||
            (!endType.IsCompatibleWith(intType) && !intType.IsCompatibleWith(endType)))
        {
            var startCommon = PrimitiveType.GetCommonType(startType, intType);
            var endCommon = PrimitiveType.GetCommonType(endType, intType);
            if (startCommon == null || endCommon == null ||
                !(startCommon is PrimitiveType startPrim && startPrim.Kind == PrimitiveKind.Integer) ||
                !(endCommon is PrimitiveType endPrim && endPrim.Kind == PrimitiveKind.Integer))
            {
                AddError(range.Line, range.Column, "Range bounds must be integer");
                return null;
            }
        }
        
        var fields = new Dictionary<string, Type>
        {
            ["start"] = intType,
            ["end"] = intType
        };
        return new RecordType(fields);
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
            if (elemType != null && !elemType.IsCompatibleWith(elementType) && !elementType.IsCompatibleWith(elemType))
            {
                var commonType = PrimitiveType.GetCommonType(elemType, elementType);
                if (commonType == null)
                {
                    AddError(arrInit.Elements[i].Line, arrInit.Elements[i].Column, 
                        $"Array element type mismatch: expected {elementType.Name}, got {elemType.Name}");
                }
            }
        }
        
        return new ArrayType(elementType, arrInit.Elements.Count);
    }
    
    private void AddError(int line, int column, string message)
    {
        _errors.Add(new SemanticError(line, column, message));
    }
}
