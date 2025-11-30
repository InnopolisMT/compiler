using Compiler.Semantic;

namespace Compiler.CodeGen;

public class LocalVariable
{
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public WasmValueType Type { get; set; }
    public bool IsParameter { get; set; }
    public int? MemoryOffset { get; set; }
    public Semantic.Type? OriginalType { get; set; } // Store the original semantic type
    public bool IsReference { get; set; } // True for parameters that are pointers to complex types
}

public class LocalsManager
{
    private readonly Dictionary<string, LocalVariable> _locals = new();
    private int _nextLocalIndex = 0;
    private int _localMemoryOffset = 0;
    private readonly MemoryManager _memoryManager;

    public LocalsManager(MemoryManager memoryManager)
    {
        _memoryManager = memoryManager;
    }

    public void Reset()
    {
        _locals.Clear();
        _nextLocalIndex = 0;
        _localMemoryOffset = 0;
    }

    public int AddParameter(string name, Semantic.Type type)
    {
        if (_memoryManager.IsComplexType(type))
        {
            // For complex types (arrays/records), the parameter is a pointer (i32)
            // It doesn't allocate new memory, just holds the address passed by the caller
            _locals[name] = new LocalVariable
            {
                Name = name,
                Index = _nextLocalIndex++,
                Type = WasmValueType.I32,
                IsParameter = true,
                IsReference = true, // Mark as reference parameter
                MemoryOffset = null, // No memory offset - the i32 holds the pointer value
                OriginalType = type
            };
        }
        else
        {
            WasmValueType wasmType = _memoryManager.GetWasmType(type);
            _locals[name] = new LocalVariable
            {
                Name = name,
                Index = _nextLocalIndex++,
                Type = wasmType,
                IsParameter = true,
                IsReference = false,
                MemoryOffset = null,
                OriginalType = type
            };
        }

        return _locals[name].Index;
    }

    public int AddLocal(string name, Semantic.Type type)
    {
        if (_memoryManager.IsComplexType(type))
        {
            int size = _memoryManager.CalculateTypeSize(type);
            int offset = _memoryManager.TotalGlobalMemorySize + _localMemoryOffset;
            _localMemoryOffset += size;

            _locals[name] = new LocalVariable
            {
                Name = name,
                Index = -1, // Not a WASM local, stored in memory
                Type = WasmValueType.I32,
                IsParameter = false,
                IsReference = false, // Local variables are not references
                MemoryOffset = offset,
                OriginalType = type
            };
            return -1; // No WASM local index for memory-backed variables
        }
        else
        {
            WasmValueType wasmType = _memoryManager.GetWasmType(type);
            _locals[name] = new LocalVariable
            {
                Name = name,
                Index = _nextLocalIndex++,
                Type = wasmType,
                IsParameter = false,
                IsReference = false,
                MemoryOffset = null,
                OriginalType = type
            };
            return _locals[name].Index;
        }
    }

    public LocalVariable? GetLocal(string name)
    {
        return _locals.TryGetValue(name, out var local) ? local : null;
    }

    public List<WasmValueType> GetNonParameterLocals()
    {
        return _locals.Values
            .Where(l => !l.IsParameter && !l.MemoryOffset.HasValue) // Only real WASM locals, not memory-backed variables
            .OrderBy(l => l.Index)
            .Select(l => l.Type)
            .ToList();
    }

    public int GetParameterCount()
    {
        return _locals.Values.Count(l => l.IsParameter);
    }

    public int AllocateTemporary(WasmValueType type)
    {
        string tempName = $"__temp_{_nextLocalIndex}";
        _locals[tempName] = new LocalVariable
        {
            Name = tempName,
            Index = _nextLocalIndex++,
            Type = type,
            IsParameter = false,
            IsReference = false,
            MemoryOffset = null
        };
        return _locals[tempName].Index;
    }
}

