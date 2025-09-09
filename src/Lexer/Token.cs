namespace Compiler.Lexer;

public class Span(int line, int start, int end)
{
    public int Line { get; } = line;
    public int Start { get; } = start;
    public int End { get; } = end;

    public override string ToString() => $"{Line}:{Start}-{End}";
}

public abstract class Token(TokenType type, string lexeme, Span span)
{
    public TokenType Type { get; } = type;
    public Span Span { get; } = span;
    public string Lexeme { get; } = lexeme;

    public override string ToString() => $"{Type}: {Lexeme}";
}

// Токен для идентификаторов
public class IdentifierToken(string lexeme, Span span) : Token(TokenType.tkIdentifier, lexeme, span)
{
    public string Name => Lexeme;
}

// Токен для целых чисел
public class IntegerToken(string lexeme, long value, Span span) : Token(TokenType.tkIntegerLiteral, lexeme, span)
{
    public long Value { get; } = value;
}

// Токен для вещественных чисел
public class RealToken(string lexeme, double value, Span span) : Token(TokenType.tkRealLiteral, lexeme, span)
{
    public double Value { get; } = value;
}

public class BooleanToken(string lexeme, bool value, Span span) : Token(TokenType.tkBoolLiteral, lexeme, span)
{
    public bool Value { get; } = value;
}

public class SimpleToken(TokenType type, string lexeme, Span span) : Token(type, lexeme, span)
{
}
