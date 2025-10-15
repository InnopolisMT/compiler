namespace Compiler.Parser;

public class LexLocation : QUT.Gppg.IMerge<LexLocation>
{
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }

    public LexLocation()
    {
    }

    public LexLocation(int sl, int sc, int el, int ec)
    {
        StartLine = sl;
        StartColumn = sc;
        EndLine = el;
        EndColumn = ec;
    }

    public LexLocation Merge(LexLocation last)
    {
        return new LexLocation(StartLine, StartColumn, last.EndLine, last.EndColumn);
    }

    public override string ToString()
    {
        return $"({StartLine},{StartColumn})-({EndLine},{EndColumn})";
    }
}

