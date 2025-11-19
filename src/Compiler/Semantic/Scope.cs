namespace Compiler.Semantic;

public class Scope
{
    public Scope? Parent { get; private set; }

    private readonly Dictionary<string, Symbol> _symbols;

    public string? Name { get; set; }

    public int Level { get; private set; }

    public Scope(Scope? parent = null, string? name = null)
    {
        Parent = parent;
        Name = name;
        Level = parent != null ? parent.Level + 1 : 0;
        _symbols = new Dictionary<string, Symbol>();
    }

    public bool Define(string name, Symbol symbol)
    {
        if (_symbols.ContainsKey(name)) return false;
        _symbols[name] = symbol;
        return true;
    }

    public Symbol? ResolveLocal(string name) => _symbols.GetValueOrDefault(name);

    public Symbol? Resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        return Parent?.Resolve(name);
    }

    public bool IsDefined(string name) => _symbols.ContainsKey(name);

    public Dictionary<string, Symbol> GetSymbols()
    {
        return new Dictionary<string, Symbol>(_symbols);
    }

    public override string ToString()
    {
        var scopeName = Name ?? "Unnamed";
        return $"Scope('{scopeName}', Level={Level}, Symbols={_symbols.Count})";
    }
}
