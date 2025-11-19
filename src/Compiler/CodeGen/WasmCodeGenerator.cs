using Compiler.AST;
using Compiler.Semantic;

namespace Compiler.CodeGen;

public class WasmCodeGenerator
{
    private readonly WasmModule _module = new();
    private readonly MemoryManager _memoryManager = new();
    private readonly Dictionary<string, int> _functionIndices = new();
    private readonly Dictionary<string, Semantic.Type?> _functionReturnTypes = new();
    private int _printIntIndex;
    private int _printRealIndex;
    private int _printBoolIndex;

    public void Generate(ProgramNode ast, string outputPath)
    {
        SetupImports();
        
        AllocateGlobalMemory(ast);
        
        ProcessRoutines(ast);
        
        _module.Memory.MinPages = _memoryManager.CalculateMemoryPages();
        
        _module.AddExport("memory", WasmExportKind.Memory, 0);
        
        var writer = new WasmBinaryWriter();
        byte[] wasmBytes = writer.Write(_module);
        File.WriteAllBytes(outputPath, wasmBytes);
        
        string directory = Path.GetDirectoryName(outputPath) ?? ".";
        string baseName = Path.GetFileNameWithoutExtension(outputPath);
        
        // Collect function information for test harness
        var routines = ast.Declarations.OfType<RoutineDeclarationNode>().ToList();
        var functionInfos = routines.Select(routine => new FunctionInfo
        {
            Name = routine.Name,
            Parameters = routine.Parameters.Select(p => (p.Name, GetTypeDisplayName(p.Type, ast))).ToList(),
            ReturnType = routine.ReturnType != null ? GetTypeDisplayName(routine.ReturnType, ast) : null
        }).ToList();
        
        var harness = new WasmTestHarness();
        harness.Generate(directory, baseName, functionInfos);
    }

    private void SetupImports()
    {
        var printIntType = new WasmFunctionType
        {
            Parameters = new List<WasmValueType> { WasmValueType.I32 },
            Results = new List<WasmValueType>()
        };
        _printIntIndex = _module.AddImport("env", "printInt", printIntType);
        _functionIndices["printInt"] = _printIntIndex;

        var printRealType = new WasmFunctionType
        {
            Parameters = new List<WasmValueType> { WasmValueType.F64 },
            Results = new List<WasmValueType>()
        };
        _printRealIndex = _module.AddImport("env", "printReal", printRealType);
        _functionIndices["printReal"] = _printRealIndex;

        var printBoolType = new WasmFunctionType
        {
            Parameters = new List<WasmValueType> { WasmValueType.I32 },
            Results = new List<WasmValueType>()
        };
        _printBoolIndex = _module.AddImport("env", "printBool", printBoolType);
        _functionIndices["printBool"] = _printBoolIndex;
    }

    private void AllocateGlobalMemory(ProgramNode ast)
    {
        foreach (var decl in ast.Declarations)
        {
            if (decl is VariableDeclarationNode varDecl)
            {
                var type = ResolveType(varDecl.Type, ast);
                _memoryManager.AllocateGlobalVariable(varDecl.Name, type);
            }
        }
    }

    private void ProcessRoutines(ProgramNode ast)
    {
        var routines = ast.Declarations.OfType<RoutineDeclarationNode>().ToList();
        
        foreach (var routine in routines)
        {
            if (routine.Body == null) continue;

            var funcType = CreateFunctionType(routine, ast);
            int funcIndex = _module.AddFunction(routine.Name, funcType);
            _functionIndices[routine.Name] = funcIndex;
            
            // Store return type for later use
            if (routine.ReturnType != null)
            {
                _functionReturnTypes[routine.Name] = ResolveType(routine.ReturnType, ast);
            }
            else
            {
                _functionReturnTypes[routine.Name] = null;
            }
        }

        foreach (var routine in routines)
        {
            if (routine.Body == null) continue;

            GenerateRoutineBody(routine, ast);
        }
    }

    private WasmFunctionType CreateFunctionType(RoutineDeclarationNode routine, ProgramNode ast)
    {
        var funcType = new WasmFunctionType();

        foreach (var param in routine.Parameters)
        {
            var paramType = ResolveType(param.Type, ast);
            if (!_memoryManager.IsComplexType(paramType))
            {
                funcType.Parameters.Add(_memoryManager.GetWasmType(paramType));
            }
        }

        if (routine.ReturnType != null)
        {
            var returnType = ResolveType(routine.ReturnType, ast);
            if (!_memoryManager.IsComplexType(returnType))
            {
                funcType.Results.Add(_memoryManager.GetWasmType(returnType));
            }
        }

        return funcType;
    }

    private void GenerateRoutineBody(RoutineDeclarationNode routine, ProgramNode ast)
    {
        if (routine.Body == null) return;

        var localsManager = new LocalsManager(_memoryManager);
        var builder = new WasmInstructionBuilder();
        var exprGen = new ExpressionCodeGen(builder, _memoryManager, localsManager, _functionIndices);
        var stmtGen = new StatementCodeGen(builder, exprGen, _memoryManager, localsManager, _functionIndices, _functionReturnTypes);

        // Set the return type for proper type conversion in return statements
        if (routine.ReturnType != null)
        {
            var returnType = ResolveType(routine.ReturnType, ast);
            stmtGen.SetCurrentFunctionReturnType(returnType);
        }

        foreach (var param in routine.Parameters)
        {
            var paramType = ResolveType(param.Type, ast);
            localsManager.AddParameter(param.Name, paramType);
        }

        ProcessDeclarations(routine.Body.Declarations, localsManager, ast);

        InitializeGlobalVariables(ast, builder, exprGen, localsManager);
        InitializeLocalVariables(routine.Body.Declarations, builder, exprGen, localsManager, ast);

        foreach (var stmt in routine.Body.Statements)
        {
            stmtGen.Generate(stmt);
        }

        if (routine.ReturnType == null)
        {
            builder.End();
        }
        else
        {
            builder.End();
        }

        int functionIndex = _functionIndices[routine.Name] - _module.Imports.Count;
        var function = _module.Functions[functionIndex];
        function.Code = builder.GetInstructions();
        function.Locals = localsManager.GetNonParameterLocals();

        _module.AddExport(routine.Name, WasmExportKind.Function, _functionIndices[routine.Name]);
    }

    private void ProcessDeclarations(List<DeclarationNode> declarations, LocalsManager localsManager, ProgramNode ast)
    {
        foreach (var decl in declarations)
        {
            if (decl is VariableDeclarationNode varDecl)
            {
                var type = ResolveType(varDecl.Type, ast);
                localsManager.AddLocal(varDecl.Name, type);
            }
        }
    }

    private void InitializeGlobalVariables(ProgramNode ast, WasmInstructionBuilder builder, ExpressionCodeGen exprGen, LocalsManager localsManager)
    {
        foreach (var decl in ast.Declarations)
        {
            if (decl is VariableDeclarationNode varDecl && varDecl.InitialValue != null)
            {
                var varInfo = _memoryManager.GetGlobalVariable(varDecl.Name);
                if (varInfo == null) continue;

                if (varInfo.IsInMemory)
                {
                    builder.I32Const(varInfo.Offset);

                    if (varDecl.InitialValue is ArrayInitializerNode arrayInit)
                    {
                        InitializeArray(arrayInit, varDecl.Type, builder, exprGen, ast);
                    }
                    else
                    {
                        exprGen.Generate(varDecl.InitialValue);
                        
                        var type = ResolveType(varDecl.Type, ast);
                        if (type is PrimitiveType pt && pt.Kind == PrimitiveKind.Real)
                        {
                            builder.F64Store();
                        }
                        else
                        {
                            builder.I32Store();
                        }
                    }
                }
            }
        }
    }

    private void InitializeLocalVariables(List<DeclarationNode> declarations, WasmInstructionBuilder builder, ExpressionCodeGen exprGen, LocalsManager localsManager, ProgramNode ast)
    {
        foreach (var decl in declarations)
        {
            if (decl is VariableDeclarationNode varDecl && varDecl.InitialValue != null)
            {
                var local = localsManager.GetLocal(varDecl.Name);
                if (local == null) continue;

                if (local.MemoryOffset.HasValue)
                {
                    builder.I32Const(local.MemoryOffset.Value);

                    if (varDecl.InitialValue is ArrayInitializerNode arrayInit)
                    {
                        InitializeArray(arrayInit, varDecl.Type, builder, exprGen, ast);
                    }
                    else
                    {
                        exprGen.Generate(varDecl.InitialValue);
                        
                        var type = ResolveType(varDecl.Type, ast);
                        if (type is PrimitiveType pt && pt.Kind == PrimitiveKind.Real)
                        {
                            builder.F64Store();
                        }
                        else
                        {
                            builder.I32Store();
                        }
                    }
                }
                else
                {
                    exprGen.Generate(varDecl.InitialValue);
                    
                    var type = ResolveType(varDecl.Type, ast);
                    if (local.Type == WasmValueType.F64 && !(type is PrimitiveType pt && pt.Kind == PrimitiveKind.Real))
                    {
                        builder.F64ConvertI32S();
                    }
                    
                    builder.LocalSet(local.Index);
                }
            }
        }
    }

    private void InitializeArray(ArrayInitializerNode arrayInit, TypeNode typeNode, WasmInstructionBuilder builder, ExpressionCodeGen exprGen, ProgramNode ast)
    {
        var arrayType = ResolveType(typeNode, ast) as ArrayType;
        if (arrayType == null)
            throw new InvalidOperationException("Array initializer used for non-array type");

        int elementSize = _memoryManager.CalculateTypeSize(arrayType.ElementType);
        bool isReal = arrayType.ElementType is PrimitiveType pt && pt.Kind == PrimitiveKind.Real;

        int baseAddrLocal = exprGen.LocalsManager.AllocateTemporary(WasmValueType.I32);
        builder.LocalSet(baseAddrLocal);

        for (int i = 0; i < arrayInit.Elements.Count; i++)
        {
            builder.LocalGet(baseAddrLocal);
            builder.I32Const(i * elementSize);
            builder.I32Add();

            exprGen.Generate(arrayInit.Elements[i]);

            if (isReal)
            {
                builder.F64Store();
            }
            else
            {
                builder.I32Store();
            }
        }
    }

    private Semantic.Type ResolveType(TypeNode typeNode, ProgramNode ast)
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
                var elementType = ResolveType(arrayType.ElementType, ast);
                int size = EvaluateConstantExpression(arrayType.Size);
                return new ArrayType(elementType, size);

            case RecordTypeNode recordType:
                var fields = new Dictionary<string, Semantic.Type>();
                foreach (var field in recordType.Fields)
                {
                    fields[field.Name] = ResolveType(field.Type, ast);
                }
                return new RecordType(fields);

            case UserTypeNode userType:
                var typeDecl = ast.Declarations.OfType<TypeDeclarationNode>()
                    .FirstOrDefault(d => d.Name == userType.TypeName);
                if (typeDecl == null)
                    throw new InvalidOperationException($"Type '{userType.TypeName}' not found");
                return ResolveType(typeDecl.Type, ast);

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
    
    private string GetTypeDisplayName(TypeNode typeNode, ProgramNode ast)
    {
        return typeNode switch
        {
            PrimitiveTypeNode pt => pt.TypeName,
            UserTypeNode ut => ut.TypeName,
            ArrayTypeNode arr => $"array[{EvaluateConstantExpression(arr.Size)}] {GetTypeDisplayName(arr.ElementType, ast)}",
            RecordTypeNode => "record",
            _ => "unknown"
        };
    }
}

