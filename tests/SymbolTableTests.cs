using Xunit;
using Compiler.Semantic;

namespace Tests;

public class SymbolTableTests
{
    [Fact]
    public void SymbolTable_InitializesWithBuiltInTypes()
    {
        // Arrange & Act
        var symbolTable = new SymbolTable();

        // Assert
        Assert.NotNull(symbolTable.Lookup("integer"));
        Assert.NotNull(symbolTable.Lookup("real"));
        Assert.NotNull(symbolTable.Lookup("boolean"));
        Assert.Equal(SymbolKind.Type, symbolTable.Lookup("integer")?.Kind);
    }

    [Fact]
    public void Enter_AddsSymbolToCurrentScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var symbol = new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer));

        // Act
        var result = symbolTable.Enter("x", symbol);

        // Assert
        Assert.True(result);
        Assert.NotNull(symbolTable.Lookup("x"));
        Assert.Equal("x", symbolTable.Lookup("x")?.Name);
    }

    [Fact]
    public void Enter_ReturnsFalseForDuplicateSymbol()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var symbol1 = new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer));
        var symbol2 = new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Real));

        // Act
        var result1 = symbolTable.Enter("x", symbol1);
        var result2 = symbolTable.Enter("x", symbol2);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.Equal("integer", symbolTable.Lookup("x")?.Type?.ToString());
    }

    [Fact]
    public void Lookup_FindsSymbolInCurrentScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));

        // Act
        var symbol = symbolTable.Lookup("x");

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("x", symbol.Name);
        Assert.Equal(SymbolKind.Variable, symbol.Kind);
    }

    [Fact]
    public void Lookup_FindsSymbolInParentScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));
        symbolTable.PushScope("inner");

        // Act
        var symbol = symbolTable.Lookup("x");

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("x", symbol.Name);
    }

    [Fact]
    public void Lookup_ReturnsNullForNonExistentSymbol()
    {
        // Arrange
        var symbolTable = new SymbolTable();

        // Act
        var symbol = symbolTable.Lookup("nonexistent");

        // Assert
        Assert.Null(symbol);
    }

    [Fact]
    public void PushScope_CreatesNewScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var initialLevel = symbolTable.CurrentLevel;

        // Act
        symbolTable.PushScope("test");

        // Assert
        Assert.Equal(initialLevel + 1, symbolTable.CurrentLevel);
    }

    [Fact]
    public void PopScope_ReturnsToParentScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.PushScope("test");
        var scopeLevel = symbolTable.CurrentLevel;

        // Act
        var result = symbolTable.PopScope();

        // Assert
        Assert.True(result);
        Assert.Equal(scopeLevel - 1, symbolTable.CurrentLevel);
    }

    [Fact]
    public void PopScope_ReturnsFalseAtGlobalScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();

        // Act
        var result = symbolTable.PopScope();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NestedScopes_SymbolsShadowing()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));

        symbolTable.PushScope("inner");
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Real)));

        // Act
        var innerX = symbolTable.Lookup("x");
        symbolTable.PopScope();
        var outerX = symbolTable.Lookup("x");

        // Assert
        Assert.Equal("real", innerX?.Type?.ToString());
        Assert.Equal("integer", outerX?.Type?.ToString());
    }

    [Fact]
    public void IsDefinedLocally_ReturnsTrueForLocalSymbol()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));

        // Act & Assert
        Assert.True(symbolTable.IsDefinedLocally("x"));
    }

    [Fact]
    public void IsDefinedLocally_ReturnsFalseForParentSymbol()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));
        symbolTable.PushScope("inner");

        // Act & Assert
        Assert.False(symbolTable.IsDefinedLocally("x"));
        Assert.True(symbolTable.IsDefined("x"));
    }

    [Fact]
    public void LookupLocal_FindsOnlyInCurrentScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));
        symbolTable.PushScope("inner");
        symbolTable.Enter("y", new Symbol("y", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Real)));

        // Act
        var y = symbolTable.LookupLocal("y");
        var x = symbolTable.LookupLocal("x");

        // Assert
        Assert.NotNull(y);
        Assert.Null(x);
    }

    [Fact]
    public void MultipleNestedScopes_WorkCorrectly()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("global", new Symbol("global", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));

        symbolTable.PushScope("level1");
        symbolTable.Enter("level1var", new Symbol("level1var", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Real)));

        symbolTable.PushScope("level2");
        symbolTable.Enter("level2var", new Symbol("level2var", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Boolean)));

        // Act & Assert
        Assert.Equal(2, symbolTable.CurrentLevel);
        Assert.NotNull(symbolTable.Lookup("global"));
        Assert.NotNull(symbolTable.Lookup("level1var"));
        Assert.NotNull(symbolTable.Lookup("level2var"));

        symbolTable.PopScope();
        Assert.Equal(1, symbolTable.CurrentLevel);
        Assert.NotNull(symbolTable.Lookup("level1var"));
        Assert.Null(symbolTable.Lookup("level2var"));

        symbolTable.PopScope();
        Assert.Equal(0, symbolTable.CurrentLevel);
        Assert.NotNull(symbolTable.Lookup("global"));
        Assert.Null(symbolTable.Lookup("level1var"));
    }

    [Fact]
    public void Reset_ClearsAllSymbolsExceptBuiltIn()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        symbolTable.Enter("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));
        symbolTable.PushScope("test");
        symbolTable.Enter("y", new Symbol("y", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Real)));

        // Act
        symbolTable.Reset();

        // Assert
        Assert.Equal(0, symbolTable.CurrentLevel);
        Assert.Null(symbolTable.Lookup("x"));
        Assert.Null(symbolTable.Lookup("y"));
        Assert.NotNull(symbolTable.Lookup("integer")); // Built-in types remain
    }

    [Fact]
    public void Symbol_StoresAttributes()
    {
        // Arrange
        var symbol = new Symbol("func", SymbolKind.Routine, new PrimitiveType(PrimitiveKind.Integer));
        symbol.Attributes["paramCount"] = 3;
        symbol.Attributes["isRecursive"] = true;

        // Act & Assert
        Assert.Equal(3, symbol.Attributes["paramCount"]);
        Assert.Equal(true, symbol.Attributes["isRecursive"]);
    }

    [Fact]
    public void Scope_TracksLevelCorrectly()
    {
        // Arrange
        var globalScope = new Scope(null, "Global");
        var level1Scope = new Scope(globalScope, "Level1");
        var level2Scope = new Scope(level1Scope, "Level2");

        // Act & Assert
        Assert.Equal(0, globalScope.Level);
        Assert.Equal(1, level1Scope.Level);
        Assert.Equal(2, level2Scope.Level);
    }

    [Fact]
    public void Scope_ResolveWorksRecursively()
    {
        // Arrange
        var globalScope = new Scope(null, "Global");
        globalScope.Define("x", new Symbol("x", SymbolKind.Variable, new PrimitiveType(PrimitiveKind.Integer)));

        var innerScope = new Scope(globalScope, "Inner");

        // Act
        var symbol = innerScope.Resolve("x");

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("x", symbol.Name);
    }
}


