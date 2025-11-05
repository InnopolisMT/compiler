namespace Compiler.Semantic;

public enum SymbolKind
{
    Variable,
    Type,
    Routine,
    Parameter
}

public class Symbol
{
    public string Name { get; set; }
    
    public SymbolKind Kind { get; set; }
    
    public Type? Type { get; set; }
    
    public Dictionary<string, object?> Attributes { get; set; }
    
    public object? DeclarationNode { get; set; }

    public Symbol(string name, SymbolKind kind)
    {
        Name = name;
        Kind = kind;
        Attributes = new Dictionary<string, object?>();
    }
    
    public Symbol(string name, SymbolKind kind, Type? type) : this(name, kind)
    {
        Type = type;
    }

    public override string ToString()
    {
        return $"Symbol({Name}, {Kind}, Type={Type})";
    }
}


