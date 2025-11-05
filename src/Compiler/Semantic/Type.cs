namespace Compiler.Semantic;

public abstract class Type
{
    public abstract string Name { get; }

    public abstract bool IsCompatibleWith(Type other);

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Type other)
        {
            return false;
        }

        return EqualsType(other);
    }

    protected abstract bool EqualsType(Type other);

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public enum PrimitiveKind
{
    Integer,
    Real,
    Boolean
}

public sealed class PrimitiveType : Type
{
    public PrimitiveKind Kind { get; }

    public PrimitiveType(PrimitiveKind kind)
    {
        Kind = kind;
    }

    public override string Name => Kind switch
    {
        PrimitiveKind.Integer => "integer",
        PrimitiveKind.Real => "real",
        PrimitiveKind.Boolean => "boolean",
        _ => "unknown"
    };

    public override bool IsCompatibleWith(Type other)
    {
        if (other is PrimitiveType p)
        {
            if (Kind == p.Kind)
            {
                return true;
            }

            // Allow implicit integer -> real compatibility
            if (Kind == PrimitiveKind.Integer && p.Kind == PrimitiveKind.Real)
            {
                return true;
            }
        }

        return false;
    }

    protected override bool EqualsType(Type other)
    {
        return other is PrimitiveType p && Kind == p.Kind;
    }
}

public sealed class ArrayType : Type
{
    public Type ElementType { get; }
    public int Size { get; }

    public ArrayType(Type elementType, int size)
    {
        ElementType = elementType;
        Size = size;
    }

    public override string Name => $"array[{Size}] of {ElementType.Name}";

    public override bool IsCompatibleWith(Type other)
    {
        if (other is ArrayType a)
        {
            // Strict compatibility: same size and compatible element types
            return Size == a.Size && ElementType.IsCompatibleWith(a.ElementType);
        }

        return false;
    }

    protected override bool EqualsType(Type other)
    {
        return other is ArrayType a && Size == a.Size && ElementType.Equals(a.ElementType);
    }
}

public sealed class RecordType : Type
{
    private readonly Dictionary<string, Type> _fields;

    public IReadOnlyDictionary<string, Type> Fields => _fields;

    public RecordType(IDictionary<string, Type> fields)
    {
        _fields = new Dictionary<string, Type>(fields);
    }

    public override string Name
    {
        get
        {
            var parts = _fields
                .OrderBy(k => k.Key)
                .Select(kvp => $"{kvp.Key}: {kvp.Value.Name}");
            return $"record {{ {string.Join(", ", parts)} }}";
        }
    }

    public override bool IsCompatibleWith(Type other)
    {
        if (other is RecordType r)
        {
            // Structural compatibility: same field set and compatible types per field
            if (_fields.Count != r._fields.Count)
            {
                return false;
            }

            foreach (var (fieldName, fieldType) in _fields)
            {
                if (!r._fields.TryGetValue(fieldName, out var otherFieldType))
                {
                    return false;
                }

                if (!fieldType.IsCompatibleWith(otherFieldType))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    protected override bool EqualsType(Type other)
    {
        if (other is not RecordType r)
        {
            return false;
        }

        if (_fields.Count != r._fields.Count)
        {
            return false;
        }

        foreach (var (fieldName, fieldType) in _fields)
        {
            if (!r._fields.TryGetValue(fieldName, out var otherFieldType))
            {
                return false;
            }

            if (!fieldType.Equals(otherFieldType))
            {
                return false;
            }
        }

        return true;
    }
}


