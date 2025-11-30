namespace Compiler.CodeGen;

public static class WasmOpcode
{
    public const byte Unreachable = 0x00;
    public const byte End = 0x0B;
    public const byte Block = 0x02;
    public const byte Loop = 0x03;
    public const byte If = 0x04;
    public const byte Else = 0x05;
    public const byte Br = 0x0C;
    public const byte BrIf = 0x0D;
    public const byte Return = 0x0F;
    public const byte Call = 0x10;
    public const byte Drop = 0x1A;

    public const byte LocalGet = 0x20;
    public const byte LocalSet = 0x21;
    public const byte LocalTee = 0x22;

    public const byte I32Load = 0x28;
    public const byte I64Load = 0x29;
    public const byte F32Load = 0x2A;
    public const byte F64Load = 0x2B;
    public const byte I32Store = 0x36;
    public const byte I64Store = 0x37;
    public const byte F32Store = 0x38;
    public const byte F64Store = 0x39;

    public const byte I32Const = 0x41;
    public const byte I64Const = 0x42;
    public const byte F32Const = 0x43;
    public const byte F64Const = 0x44;

    public const byte I32Eqz = 0x45;
    public const byte I32Eq = 0x46;
    public const byte I32Ne = 0x47;
    public const byte I32LtS = 0x48;
    public const byte I32LtU = 0x49;
    public const byte I32GtS = 0x4A;
    public const byte I32GtU = 0x4B;
    public const byte I32LeS = 0x4C;
    public const byte I32LeU = 0x4D;
    public const byte I32GeS = 0x4E;
    public const byte I32GeU = 0x4F;

    public const byte F64Eq = 0x61;
    public const byte F64Ne = 0x62;
    public const byte F64Lt = 0x63;
    public const byte F64Gt = 0x64;
    public const byte F64Le = 0x65;
    public const byte F64Ge = 0x66;

    public const byte I32Add = 0x6A;
    public const byte I32Sub = 0x6B;
    public const byte I32Mul = 0x6C;
    public const byte I32DivS = 0x6D;
    public const byte I32DivU = 0x6E;
    public const byte I32RemS = 0x6F;
    public const byte I32RemU = 0x70;
    public const byte I32And = 0x71;
    public const byte I32Or = 0x72;
    public const byte I32Xor = 0x73;

    public const byte F64Add = 0xA0;
    public const byte F64Sub = 0xA1;
    public const byte F64Mul = 0xA2;
    public const byte F64Div = 0xA3;

    public const byte I32WrapI64 = 0xA7;
    public const byte I32TruncF64S = 0xAA;
    public const byte I64ExtendI32S = 0xAC;
    public const byte F64ConvertI32S = 0xB7;
    public const byte F64ConvertI32U = 0xB8;

    // Bulk memory prefix and subopcodes
    public const byte BulkMemoryPrefix = 0xFC; // prefix for bulk memory ops
    public const byte MemCopy = 0x0A;          // subopcode for memory.copy
}

public class WasmInstructionBuilder
{
    private readonly List<byte> _instructions = new();

    public List<byte> GetInstructions() => _instructions;

    public void Clear() => _instructions.Clear();

    public void End() => _instructions.Add(WasmOpcode.End);

    public void Block(WasmValueType blockType = WasmValueType.Void)
    {
        _instructions.Add(WasmOpcode.Block);
        _instructions.Add((byte)blockType);
    }

    public void Loop(WasmValueType blockType = WasmValueType.Void)
    {
        _instructions.Add(WasmOpcode.Loop);
        _instructions.Add((byte)blockType);
    }

    public void If(WasmValueType blockType = WasmValueType.Void)
    {
        _instructions.Add(WasmOpcode.If);
        _instructions.Add((byte)blockType);
    }

    public void Else() => _instructions.Add(WasmOpcode.Else);

    public void Br(int labelIndex)
    {
        _instructions.Add(WasmOpcode.Br);
        WriteLEB128Unsigned((uint)labelIndex);
    }

    public void BrIf(int labelIndex)
    {
        _instructions.Add(WasmOpcode.BrIf);
        WriteLEB128Unsigned((uint)labelIndex);
    }

    public void Return() => _instructions.Add(WasmOpcode.Return);
    
    public void Unreachable() => _instructions.Add(WasmOpcode.Unreachable);
    
    public void Drop() => _instructions.Add(WasmOpcode.Drop);

    public void Call(int functionIndex)
    {
        _instructions.Add(WasmOpcode.Call);
        WriteLEB128Unsigned((uint)functionIndex);
    }

    public void LocalGet(int localIndex)
    {
        _instructions.Add(WasmOpcode.LocalGet);
        WriteLEB128Unsigned((uint)localIndex);
    }

    public void LocalSet(int localIndex)
    {
        _instructions.Add(WasmOpcode.LocalSet);
        WriteLEB128Unsigned((uint)localIndex);
    }

    public void LocalTee(int localIndex)
    {
        _instructions.Add(WasmOpcode.LocalTee);
        WriteLEB128Unsigned((uint)localIndex);
    }

    public void I32Const(int value)
    {
        _instructions.Add(WasmOpcode.I32Const);
        WriteLEB128Signed(value);
    }

    public void I64Const(long value)
    {
        _instructions.Add(WasmOpcode.I64Const);
        WriteLEB128Signed(value);
    }

    public void F64Const(double value)
    {
        _instructions.Add(WasmOpcode.F64Const);
        byte[] bytes = BitConverter.GetBytes(value);
        _instructions.AddRange(bytes);
    }

    public void I32Load(int offset = 0, int align = 2)
    {
        _instructions.Add(WasmOpcode.I32Load);
        WriteLEB128Unsigned((uint)align);
        WriteLEB128Unsigned((uint)offset);
    }

    public void F64Load(int offset = 0, int align = 3)
    {
        _instructions.Add(WasmOpcode.F64Load);
        WriteLEB128Unsigned((uint)align);
        WriteLEB128Unsigned((uint)offset);
    }

    public void I32Store(int offset = 0, int align = 2)
    {
        _instructions.Add(WasmOpcode.I32Store);
        WriteLEB128Unsigned((uint)align);
        WriteLEB128Unsigned((uint)offset);
    }

    public void F64Store(int offset = 0, int align = 3)
    {
        _instructions.Add(WasmOpcode.F64Store);
        WriteLEB128Unsigned((uint)align);
        WriteLEB128Unsigned((uint)offset);
    }

    public void I32Add() => _instructions.Add(WasmOpcode.I32Add);
    public void I32Sub() => _instructions.Add(WasmOpcode.I32Sub);
    public void I32Mul() => _instructions.Add(WasmOpcode.I32Mul);
    public void I32DivS() => _instructions.Add(WasmOpcode.I32DivS);
    public void I32RemS() => _instructions.Add(WasmOpcode.I32RemS);
    public void I32And() => _instructions.Add(WasmOpcode.I32And);
    public void I32Or() => _instructions.Add(WasmOpcode.I32Or);
    public void I32Xor() => _instructions.Add(WasmOpcode.I32Xor);

    public void I32Eq() => _instructions.Add(WasmOpcode.I32Eq);
    public void I32Ne() => _instructions.Add(WasmOpcode.I32Ne);
    public void I32LtS() => _instructions.Add(WasmOpcode.I32LtS);
    public void I32GtS() => _instructions.Add(WasmOpcode.I32GtS);
    public void I32LeS() => _instructions.Add(WasmOpcode.I32LeS);
    public void I32GeS() => _instructions.Add(WasmOpcode.I32GeS);

    public void I32Eqz() => _instructions.Add(WasmOpcode.I32Eqz);

    public void F64Add() => _instructions.Add(WasmOpcode.F64Add);
    public void F64Sub() => _instructions.Add(WasmOpcode.F64Sub);
    public void F64Mul() => _instructions.Add(WasmOpcode.F64Mul);
    public void F64Div() => _instructions.Add(WasmOpcode.F64Div);

    public void F64Eq() => _instructions.Add(WasmOpcode.F64Eq);
    public void F64Ne() => _instructions.Add(WasmOpcode.F64Ne);
    public void F64Lt() => _instructions.Add(WasmOpcode.F64Lt);
    public void F64Gt() => _instructions.Add(WasmOpcode.F64Gt);
    public void F64Le() => _instructions.Add(WasmOpcode.F64Le);
    public void F64Ge() => _instructions.Add(WasmOpcode.F64Ge);

    public void F64ConvertI32S() => _instructions.Add(WasmOpcode.F64ConvertI32S);
    public void I32TruncF64S() => _instructions.Add(WasmOpcode.I32TruncF64S);

    // memory.copy: pops dest, src, len (i32) and copies len bytes
    // Encoded as: 0xFC 0x0A <destMemIdx=0> <srcMemIdx=0>
    public void MemoryCopy()
    {
        _instructions.Add(WasmOpcode.BulkMemoryPrefix);
        WriteLEB128Unsigned(WasmOpcode.MemCopy);
        // memory indices (both zero for the default linear memory)
        WriteLEB128Unsigned(0);
        WriteLEB128Unsigned(0);
    }

    private void WriteLEB128Unsigned(uint value)
    {
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
            {
                b |= 0x80;
            }
            _instructions.Add(b);
        } while (value != 0);
    }

    private void WriteLEB128Signed(long value)
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

            _instructions.Add(b);
        }
    }
}

