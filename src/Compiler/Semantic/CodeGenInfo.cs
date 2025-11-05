namespace Compiler.Semantic;

public class CodeGenInfo
{
    public int? Address { get; set; }
    public int? Offset { get; set; }
    public string? Register { get; set; }
    public bool IsParameter { get; set; }
    public int ParameterIndex { get; set; }
    public int? FrameOffset { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsLocal { get; set; }
    public Dictionary<string, object?> Attributes { get; set; }

    public CodeGenInfo()
    {
        Attributes = new Dictionary<string, object?>();
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Address.HasValue)
            parts.Add($"Address={Address}");
        if (Offset.HasValue)
            parts.Add($"Offset={Offset}");
        if (FrameOffset.HasValue)
            parts.Add($"FrameOffset={FrameOffset}");
        if (!string.IsNullOrEmpty(Register))
            parts.Add($"Register={Register}");
        if (IsParameter)
            parts.Add($"ParameterIndex={ParameterIndex}");
        if (IsGlobal)
            parts.Add("Global");
        if (IsLocal)
            parts.Add("Local");

        return parts.Count > 0 ? string.Join(", ", parts) : "Empty";
    }
}

