using Compiler.Semantic;

namespace Compiler.CodeGen;

public class MemoryLocation
{
    public int Offset { get; set; }
    public int Size { get; set; }
    public bool IsInMemory { get; set; }
    public int? LocalIndex { get; set; }
    public Semantic.Type? OriginalType { get; set; } // Store the original semantic type
}

public class MemoryManager
{
    private int _globalOffset = 0;
    private readonly Dictionary<string, MemoryLocation> _globalVariables = new();
    private readonly Dictionary<string, int> _recordFieldOffsets = new();

    public int TotalGlobalMemorySize => _globalOffset;

    public int CalculateTypeSize(Semantic.Type type)
    {
        return type switch
        {
            PrimitiveType pt => pt.Kind switch
            {
                PrimitiveKind.Integer => 4,
                PrimitiveKind.Boolean => 4,
                PrimitiveKind.Real => 8,
                _ => throw new InvalidOperationException($"Unknown primitive type: {pt.Kind}")
            },
            ArrayType at => at.Size * CalculateTypeSize(at.ElementType),
            RecordType rt => CalculateRecordSize(rt),
            _ => throw new InvalidOperationException($"Unknown type: {type}")
        };
    }

    private int CalculateRecordSize(RecordType recordType)
    {
        int totalSize = 0;
        foreach (var field in recordType.Fields)
        {
            totalSize += CalculateTypeSize(field.Value);
        }
        return totalSize;
    }

    public void AllocateGlobalVariable(string name, Semantic.Type type)
    {
        int size = CalculateTypeSize(type);

        _globalVariables[name] = new MemoryLocation
        {
            Offset = _globalOffset,
            Size = size,
            IsInMemory = true,
            LocalIndex = null,
            OriginalType = type
        };

        _globalOffset += size;
    }

    public MemoryLocation? GetGlobalVariable(string name)
    {
        return _globalVariables.TryGetValue(name, out var location) ? location : null;
    }

    public bool IsComplexType(Semantic.Type type)
    {
        return type is ArrayType or RecordType;
    }

    public int CalculateFieldOffset(RecordType recordType, string fieldName)
    {
        string key = $"{recordType.GetHashCode()}:{fieldName}";
        
        if (_recordFieldOffsets.TryGetValue(key, out int cachedOffset))
        {
            return cachedOffset;
        }

        int offset = 0;
        foreach (var field in recordType.Fields)
        {
            if (field.Key == fieldName)
            {
                _recordFieldOffsets[key] = offset;
                return offset;
            }
            offset += CalculateTypeSize(field.Value);
        }

        throw new InvalidOperationException($"Field '{fieldName}' not found in record type");
    }

    public WasmValueType GetWasmType(Semantic.Type type)
    {
        return type switch
        {
            PrimitiveType pt => pt.Kind switch
            {
                PrimitiveKind.Integer => WasmValueType.I32,
                PrimitiveKind.Boolean => WasmValueType.I32,
                PrimitiveKind.Real => WasmValueType.F64,
                _ => throw new InvalidOperationException($"Unknown primitive type: {pt.Kind}")
            },
            _ => throw new InvalidOperationException($"Complex types cannot be directly converted to WASM types")
        };
    }

    public int CalculateMemoryPages()
    {
        int bytesNeeded = _globalOffset;
        int pagesNeeded = (bytesNeeded + 65535) / 65536;
        return Math.Max(1, pagesNeeded);
    }
}

