namespace Compiler.CodeGen;

public class WasmBinaryWriter
{
    private readonly List<byte> _buffer = new();

    public byte[] Write(WasmModule module)
    {
        _buffer.Clear();

        WriteMagicAndVersion();

        WriteTypeSection(module);
        WriteImportSection(module);
        WriteFunctionSection(module);
        WriteMemorySection(module);
        WriteExportSection(module);
        WriteCodeSection(module);

        return _buffer.ToArray();
    }

    private void WriteMagicAndVersion()
    {
        _buffer.Add(0x00);
        _buffer.Add(0x61);
        _buffer.Add(0x73);
        _buffer.Add(0x6D);

        _buffer.Add(0x01);
        _buffer.Add(0x00);
        _buffer.Add(0x00);
        _buffer.Add(0x00);
    }

    private void WriteTypeSection(WasmModule module)
    {
        if (module.Types.Count == 0) return;

        var sectionData = new List<byte>();
        WriteLEB128Unsigned(sectionData, (uint)module.Types.Count);

        foreach (var type in module.Types)
        {
            sectionData.Add(0x60);

            WriteLEB128Unsigned(sectionData, (uint)type.Parameters.Count);
            foreach (var param in type.Parameters)
            {
                sectionData.Add((byte)param);
            }

            WriteLEB128Unsigned(sectionData, (uint)type.Results.Count);
            foreach (var result in type.Results)
            {
                sectionData.Add((byte)result);
            }
        }

        WriteSection(1, sectionData);
    }

    private void WriteImportSection(WasmModule module)
    {
        if (module.Imports.Count == 0) return;

        var sectionData = new List<byte>();
        WriteLEB128Unsigned(sectionData, (uint)module.Imports.Count);

        foreach (var import in module.Imports)
        {
            WriteString(sectionData, import.Module);
            WriteString(sectionData, import.Name);

            sectionData.Add(0x00);
            WriteLEB128Unsigned(sectionData, (uint)import.TypeIndex);
        }

        WriteSection(2, sectionData);
    }

    private void WriteFunctionSection(WasmModule module)
    {
        if (module.Functions.Count == 0) return;

        var sectionData = new List<byte>();
        WriteLEB128Unsigned(sectionData, (uint)module.Functions.Count);

        foreach (var func in module.Functions)
        {
            WriteLEB128Unsigned(sectionData, (uint)func.TypeIndex);
        }

        WriteSection(3, sectionData);
    }

    private void WriteMemorySection(WasmModule module)
    {
        var sectionData = new List<byte>();
        WriteLEB128Unsigned(sectionData, 1);

        if (module.Memory.MaxPages.HasValue)
        {
            sectionData.Add(0x01);
            WriteLEB128Unsigned(sectionData, (uint)module.Memory.MinPages);
            WriteLEB128Unsigned(sectionData, (uint)module.Memory.MaxPages.Value);
        }
        else
        {
            sectionData.Add(0x00);
            WriteLEB128Unsigned(sectionData, (uint)module.Memory.MinPages);
        }

        WriteSection(5, sectionData);
    }

    private void WriteExportSection(WasmModule module)
    {
        if (module.Exports.Count == 0) return;

        var sectionData = new List<byte>();
        WriteLEB128Unsigned(sectionData, (uint)module.Exports.Count);

        foreach (var export in module.Exports)
        {
            WriteString(sectionData, export.Name);
            sectionData.Add((byte)export.Kind);
            WriteLEB128Unsigned(sectionData, (uint)export.Index);
        }

        WriteSection(7, sectionData);
    }

    private void WriteCodeSection(WasmModule module)
    {
        if (module.Functions.Count == 0) return;

        var sectionData = new List<byte>();
        WriteLEB128Unsigned(sectionData, (uint)module.Functions.Count);

        foreach (var func in module.Functions)
        {
            var funcBody = new List<byte>();

            var localGroups = CompressLocals(func.Locals);
            WriteLEB128Unsigned(funcBody, (uint)localGroups.Count);

            foreach (var (type, count) in localGroups)
            {
                WriteLEB128Unsigned(funcBody, (uint)count);
                funcBody.Add((byte)type);
            }

            funcBody.AddRange(func.Code);

            WriteLEB128Unsigned(sectionData, (uint)funcBody.Count);
            sectionData.AddRange(funcBody);
        }

        WriteSection(10, sectionData);
    }

    private List<(WasmValueType type, int count)> CompressLocals(List<WasmValueType> locals)
    {
        var result = new List<(WasmValueType, int)>();
        if (locals.Count == 0) return result;

        WasmValueType currentType = locals[0];
        int count = 1;

        for (int i = 1; i < locals.Count; i++)
        {
            if (locals[i] == currentType)
            {
                count++;
            }
            else
            {
                result.Add((currentType, count));
                currentType = locals[i];
                count = 1;
            }
        }

        result.Add((currentType, count));
        return result;
    }

    private void WriteSection(byte sectionId, List<byte> sectionData)
    {
        _buffer.Add(sectionId);
        WriteLEB128Unsigned(_buffer, (uint)sectionData.Count);
        _buffer.AddRange(sectionData);
    }

    private void WriteString(List<byte> buffer, string str)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(str);
        WriteLEB128Unsigned(buffer, (uint)bytes.Length);
        buffer.AddRange(bytes);
    }

    private void WriteLEB128Unsigned(List<byte> buffer, uint value)
    {
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
            {
                b |= 0x80;
            }
            buffer.Add(b);
        } while (value != 0);
    }

    private void WriteLEB128Signed(List<byte> buffer, long value)
    {
        bool more = true;
        while (more)
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;

            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
            {
                more = false;
            }
            else
            {
                b |= 0x80;
            }

            buffer.Add(b);
        }
    }
}

