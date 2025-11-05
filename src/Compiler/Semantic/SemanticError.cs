namespace Compiler.Semantic;

public class SemanticError
{
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }

    public SemanticError(int line, int column, string message)
    {
        Line = line;
        Column = column;
        Message = message;
    }

    public string Format(string? fileName = null)
    {
        var location = fileName != null
            ? $"{fileName}({Line},{Column})"
            : $"({Line},{Column})";
        return $"{location}: {Message}";
    }

    public override string ToString()
    {
        return Format();
    }
}
