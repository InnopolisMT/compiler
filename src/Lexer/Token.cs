namespace Compiler.Lexer;

public class Span(int line, int posStart, int posEnd)
{
    public int Line { get; set; } = line;
    public int PosStart { get; set; } = posStart;
    public int PosEnd { get; set; } = posEnd;
}

public class Token(TokenType type, string value, Span span)
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public Span Span { get; } = span;

    public override string ToString() => $"{Type}: {Value}";
}
