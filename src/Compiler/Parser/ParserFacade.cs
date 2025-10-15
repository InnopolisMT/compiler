using Compiler.AST;
using Compiler.Lexer;

namespace Compiler.Parser;
public class ParserFacade
{
    private readonly LexerClass _lexer;

    public ParserFacade(LexerClass lexer)
    {
        _lexer = lexer;
    }

    public ProgramNode Parse()
    {
        var adapter = new LexerAdapter(_lexer);
        var parser = new Parser(adapter);

        if (!parser.Parse())
        {
            throw new ParseException("Parsing failed");
        }

        return parser.Result!;
    }
}

