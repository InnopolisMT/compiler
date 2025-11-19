namespace Compiler.CodeGen;

public enum WasmValueType : byte
{
    I32 = 0x7F,
    I64 = 0x7E,
    F32 = 0x7D,
    F64 = 0x7C,
    Void = 0x40
}

public class WasmFunctionType
{
    public List<WasmValueType> Parameters { get; set; } = new();
    public List<WasmValueType> Results { get; set; } = new();

    public override bool Equals(object? obj)
    {
        if (obj is not WasmFunctionType other) return false;
        return Parameters.SequenceEqual(other.Parameters) && Results.SequenceEqual(other.Results);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var p in Parameters) hash.Add(p);
        foreach (var r in Results) hash.Add(r);
        return hash.ToHashCode();
    }
}

public class WasmImport
{
    public string Module { get; set; } = "";
    public string Name { get; set; } = "";
    public int TypeIndex { get; set; }
}

public class WasmFunction
{
    public string Name { get; set; } = "";
    public int TypeIndex { get; set; }
    public List<WasmValueType> Locals { get; set; } = new();
    public List<byte> Code { get; set; } = new();
}

public class WasmExport
{
    public string Name { get; set; } = "";
    public WasmExportKind Kind { get; set; }
    public int Index { get; set; }
}

public enum WasmExportKind : byte
{
    Function = 0x00,
    Table = 0x01,
    Memory = 0x02,
    Global = 0x03
}

public class WasmMemory
{
    public int MinPages { get; set; } = 1;
    public int? MaxPages { get; set; }
}

public class WasmModule
{
    public List<WasmFunctionType> Types { get; set; } = new();
    public List<WasmImport> Imports { get; set; } = new();
    public List<WasmFunction> Functions { get; set; } = new();
    public WasmMemory Memory { get; set; } = new();
    public List<WasmExport> Exports { get; set; } = new();

    public int AddType(WasmFunctionType type)
    {
        int index = Types.FindIndex(t => t.Equals(type));
        if (index >= 0) return index;
        
        Types.Add(type);
        return Types.Count - 1;
    }

    public int AddImport(string module, string name, WasmFunctionType type)
    {
        int typeIndex = AddType(type);
        Imports.Add(new WasmImport 
        { 
            Module = module, 
            Name = name, 
            TypeIndex = typeIndex 
        });
        return Imports.Count - 1;
    }

    public int AddFunction(string name, WasmFunctionType type)
    {
        int typeIndex = AddType(type);
        Functions.Add(new WasmFunction 
        { 
            Name = name, 
            TypeIndex = typeIndex 
        });
        return Imports.Count + Functions.Count - 1;
    }

    public void AddExport(string name, WasmExportKind kind, int index)
    {
        Exports.Add(new WasmExport 
        { 
            Name = name, 
            Kind = kind, 
            Index = index 
        });
    }
}

