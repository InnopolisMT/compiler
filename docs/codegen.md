# Code Generation

## Team
Our team is **Tourists**  
Team members: **Danila Khrankou & Tsimafei Kurstak**

## WebAssembly Platform Overview

WebAssembly (WASM) is a binary instruction format designed as a portable compilation target for high-level languages. Key characteristics:

- **Stack-based virtual machine**: Operations use a value stack (push operands, pop results)
- **Typed instructions**: Each instruction has explicit types (i32, i64, f32, f64)
- **Linear memory**: Single contiguous address space accessible via load/store instructions
- **Module structure**: Organized into sections (types, imports, functions, memory, exports, code)
- **Binary format**: Compact representation using LEB128 variable-length encoding
- **Security**: Sandboxed execution environment with no direct access to host system

### WASM Value Types
- `i32`: 32-bit integer
- `i64`: 64-bit integer  
- `f32`: 32-bit float
- `f64`: 64-bit float

### Memory Model
- Linear memory is a contiguous byte array
- Access via `i32.load`, `i32.store`, `f64.load`, `f64.store` with offset/alignment
- Memory size specified in pages (64KB each)
- No pointers: addresses are computed as offsets from base

### Instruction Format
- Opcodes are single bytes (e.g., `i32.add` = 0x6A)
- Immediate operands encoded in LEB128 format
- Control flow via `block`, `loop`, `if`, `br`, `br_if`
- Function calls via `call` with function index

## Implementation
- **Language**: ProjectI (Imperative)
- **Implementation**: C#
- **Module**: WASM code generator (`WasmCodeGenerator`) with binary format construction
- **Target platform**: WebAssembly (WASM)

## What the code generator does
The code generator transforms a semantically validated AST into an executable WASM module:
- **Expressions**: instruction generation for literals, operations, function calls, array and record access
- **Statements**: assignments, conditionals, loops, returns, print statements
- **Memory**: allocation of global and local variables, management of complex types (arrays, records)
- **Binary format**: writing the WASM module to binary format according to the specification

## Generator Architecture

The generator consists of several components:

```csharp
public class WasmCodeGenerator
{
    private readonly WasmModule _module = new();
    private readonly MemoryManager _memoryManager = new();
    private readonly Dictionary<string, int> _functionIndices = new();
    
    public void Generate(ProgramNode ast, string outputPath)
    {
        SetupImports();
        AllocateGlobalMemory(ast);
        ProcessRoutines(ast);
        // ...
        var writer = new WasmBinaryWriter();
        byte[] wasmBytes = writer.Write(_module);
        File.WriteAllBytes(outputPath, wasmBytes);
    }
}
```

### Components

- **WasmCodeGenerator**: main coordinator, manages the generation process
- **ExpressionCodeGen**: generates code for expressions
- **StatementCodeGen**: generates code for statements
- **WasmInstructionBuilder**: builds WASM bytecode sequences
- **MemoryManager**: manages memory allocation for global variables
- **WasmBinaryWriter**: writes the module to WASM binary format

## Generation Process

### 1. Setting up imports
External functions for output (printInt, printReal, printBool) are registered:

```csharp
private void SetupImports()
{
    var printIntType = new WasmFunctionType
    {
        Parameters = new List<WasmValueType> { WasmValueType.I32 },
        Results = new List<WasmValueType>()
    };
    _printIntIndex = _module.AddImport("env", "printInt", printIntType);
}
```

### 2. Global memory allocation
All global variables are placed in linear memory:

```csharp
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
```

`MemoryManager` calculates type sizes and offsets:
- `integer`, `boolean`: 4 bytes
- `real`: 8 bytes
- `array[N] of T`: N × size(T)
- `record`: sum of field sizes

### 3. Processing routines
Function signatures are registered first, then bodies are generated:

```csharp
private void ProcessRoutines(ProgramNode ast)
{
    // First pass: register functions
    foreach (var routine in routines)
    {
        var funcType = CreateFunctionType(routine, ast);
        int funcIndex = _module.AddFunction(routine.Name, funcType);
        _functionIndices[routine.Name] = funcIndex;
    }
    
    // Second pass: generate bodies
    foreach (var routine in routines)
    {
        GenerateRoutineBody(routine, ast);
    }
}
```

### 4. Expression generation

`ExpressionCodeGen` transforms AST nodes into WASM instruction sequences:

```csharp
public void Generate(ExpressionNode expr)
{
    switch (expr)
    {
        case IntegerLiteralNode intLit:
            _builder.I32Const((int)intLit.Value);
            break;
        case BinaryOperationNode binOp:
            GenerateBinaryOperation(binOp);
            break;
        // ...
    }
}
```

Example: `x + y * 2`
```csharp
// Generate x
_builder.I32Const(offset_x);
_builder.I32Load();

// Generate y
_builder.I32Const(offset_y);
_builder.I32Load();

// Generate 2
_builder.I32Const(2);

// Multiply y * 2
_builder.I32Mul();

// Add x + (y * 2)
_builder.I32Add();
```

For operations with `real`, type conversions are automatically added:
```csharp
if (isReal)
{
    if (!IsRealType(leftType))
        _builder.F64ConvertI32S(); // int → real
    GenerateRealBinaryOperation(node.Operator);
}
```

### 5. Statement generation

`StatementCodeGen` handles statements:

**Assignment:**
```csharp
private void GenerateAssignment(AssignmentNode node)
{
    _exprGen.GenerateAddress(node.Target);
    _exprGen.Generate(node.Value);
    
    if (IsRealType(targetType))
        _builder.F64Store();
    else
        _builder.I32Store();
}
```

**Conditional statement:**
```csharp
private void GenerateIfStatement(IfStatementNode node)
{
    _exprGen.Generate(node.Condition);
    _builder.If();
    
    foreach (var stmt in node.ThenBody)
        Generate(stmt);
    
    if (node.ElseBody.Count > 0)
    {
        _builder.Else();
        foreach (var stmt in node.ElseBody)
            Generate(stmt);
    }
    
    _builder.End();
}
```

**While loop:**
```csharp
private void GenerateWhileLoop(WhileLoopNode node)
{
    _builder.Block();
    _builder.Loop();
    
    _exprGen.Generate(node.Condition);
    _builder.I32Eqz();
    _builder.BrIf(1); // exit block
    
    foreach (var stmt in node.Body)
        Generate(stmt);
    
    _builder.Br(0); // return to loop start
    _builder.End();
    _builder.End();
}
```

### 6. Instruction building

`WasmInstructionBuilder` creates bytecode sequences:

```csharp
public class WasmInstructionBuilder
{
    private readonly List<byte> _instructions = new();
    
    public void I32Const(int value)
    {
        _instructions.Add(WasmOpcode.I32Const);
        WriteLEB128Signed(value);
    }
    
    public void I32Add() => _instructions.Add(WasmOpcode.I32Add);
    
    public void LocalGet(int localIndex)
    {
        _instructions.Add(WasmOpcode.LocalGet);
        WriteLEB128Unsigned((uint)localIndex);
    }
}
```

LEB128 is used for variable-length number encoding in the binary format.

### 7. Binary format writing

`WasmBinaryWriter` writes the module to WASM binary format:

```csharp
public byte[] Write(WasmModule module)
{
    WriteMagicAndVersion();      // \0asm + version
    WriteTypeSection(module);     // function types
    WriteImportSection(module);   // imports
    WriteFunctionSection(module); // function indices
    WriteMemorySection(module);  // memory
    WriteExportSection(module);   // exports
    WriteCodeSection(module);    // bytecode
    
    return _buffer.ToArray();
}
```

Each section is written in the format:
- Section ID (1 byte)
- Data size (LEB128)
- Section data

**Code section example:**
```csharp
private void WriteCodeSection(WasmModule module)
{
    foreach (var func in module.Functions)
    {
        var funcBody = new List<byte>();
        
        // Local variables (compressed by type)
        var localGroups = CompressLocals(func.Locals);
        WriteLEB128Unsigned(funcBody, (uint)localGroups.Count);
        foreach (var (type, count) in localGroups)
        {
            WriteLEB128Unsigned(funcBody, (uint)count);
            funcBody.Add((byte)type);
        }
        
        // Function bytecode
        funcBody.AddRange(func.Code);
        
        // Function body size
        WriteLEB128Unsigned(sectionData, (uint)funcBody.Count);
        sectionData.AddRange(funcBody);
    }
}
```

## Generation Examples

### Simple expression
Source code: `var x : integer is 42`

Generated code:
```
i32.const <offset_x>  // variable address
i32.const 42          // value
i32.store             // store
```

### Binary operation
Source code: `result := a + b`

Generated code:
```
i32.const <offset_a>
i32.load              // load a
i32.const <offset_b>
i32.load              // load b
i32.add               // add
i32.const <offset_result>
i32.store             // store
```

### Array access
Source code: `arr[i]`

Generated code:
```
i32.const <offset_arr>  // array base address
i32.const <i>           // index
i32.const 1
i32.sub                 // index - 1 (1-based → 0-based)
i32.const <elementSize>
i32.mul                 // offset = (i-1) * elementSize
i32.add                 // element address
i32.load                // load value
```

### Record field access
Source code: `person.age`

Generated code:
```
i32.const <offset_person>  // record address
i32.const <fieldOffset>    // age field offset
i32.add                    // field address
i32.load                   // load value
```

## Implementation Details

### Memory management
- Primitive types (`integer`, `boolean`, `real`) can be stored in WASM local variables for efficiency
- Complex types (arrays, records) are always placed in linear memory
- Global variables are placed at the start of memory, locals follow them

### Type conversion
- Automatic widening `integer` → `real` in arithmetic operations
- Explicit conversion on assignment and return values
- Type-aware instruction selection for load/store (`i32.load` vs `f64.load`)

### Optimizations
- Local variable compression by type in binary format (grouping same types)
- Using WASM locals for primitives instead of memory
- Minimal memory allocation (precise size calculation)

## Result

The generator creates an executable `.wasm` file that:
- Complies with WebAssembly 1.0 specification
- Can be loaded and executed in a WASM runtime
- Exports functions and memory for interaction with host environment
- Uses efficient data and instruction representation
