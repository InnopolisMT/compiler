namespace Compiler.Semantic;

public class SymbolTable
{
    private Scope _currentScope;

    private readonly Stack<Scope> _scopeStack;

    public Scope GlobalScope { get; private set; }

    public Scope CurrentScope => _currentScope;

    public int CurrentLevel => _currentScope.Level;

    public SymbolTable()
    {
        GlobalScope = new Scope(null, "Global");
        _currentScope = GlobalScope;
        _scopeStack = new Stack<Scope>();
        _scopeStack.Push(GlobalScope);

        InitializeBuiltInTypes();
    }

    private void InitializeBuiltInTypes()
    {
        Enter("integer", new Symbol("integer", SymbolKind.Type, new PrimitiveType(PrimitiveKind.Integer)));
        Enter("real", new Symbol("real", SymbolKind.Type, new PrimitiveType(PrimitiveKind.Real)));
        Enter("boolean", new Symbol("boolean", SymbolKind.Type, new PrimitiveType(PrimitiveKind.Boolean)));
    }

    public bool Enter(string name, Symbol symbol)
    {
        return _currentScope.Define(name, symbol);
    }

    public Symbol? Lookup(string name)
    {
        return _currentScope.Resolve(name);
    }

    public Symbol? LookupLocal(string name)
    {
        return _currentScope.ResolveLocal(name);
    }

    public void PushScope(string? scopeName = null)
    {
        var newScope = new Scope(_currentScope, scopeName);
        _scopeStack.Push(newScope);
        _currentScope = newScope;
    }

    public bool PopScope()
    {
        if (_scopeStack.Count <= 1)
        {
            return false;
        }

        _scopeStack.Pop();
        _currentScope = _scopeStack.Peek();
        return true;
    }

    public bool IsDefinedLocally(string name)
    {
        return _currentScope.IsDefined(name);
    }

    public bool IsDefined(string name)
    {
        return Lookup(name) != null;
    }

    public Dictionary<string, Symbol> GetCurrentScopeSymbols()
    {
        return _currentScope.GetSymbols();
    }

    public void Reset()
    {
        _scopeStack.Clear();
        GlobalScope = new Scope(null, "Global");
        _currentScope = GlobalScope;
        _scopeStack.Push(GlobalScope);
        InitializeBuiltInTypes();
    }

    public override string ToString()
    {
        return $"SymbolTable(CurrentScope={_currentScope}, Level={CurrentLevel}, ScopeCount={_scopeStack.Count})";
    }

    public string DumpScopes()
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine("Symbol Table Hierarchy:");
        result.AppendLine("======================");

        var scopes = _scopeStack.Reverse().ToList();
        for (int i = 0; i < scopes.Count; i++)
        {
            var scope = scopes[i];
            var indent = new string(' ', scope.Level * 2);
            result.AppendLine($"{indent}{scope}");

            var symbols = scope.GetSymbols();
            foreach (var kvp in symbols)
            {
                result.AppendLine($"{indent}  - {kvp.Value}");
            }
        }

        return result.ToString();
    }
}
