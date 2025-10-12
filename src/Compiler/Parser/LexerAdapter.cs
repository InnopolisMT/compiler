using Compiler.Lexer;
using QUT.Gppg;

namespace Compiler.Parser;

/// <summary>
/// Adapter between custom lexer and gppg parser
/// </summary>
internal class LexerAdapter : AbstractScanner<object, Compiler.Parser.LexLocation>
{
    private readonly LexerClass _lexer;
    private Token _currentToken;

    // Mapping from lexer TokenType to parser Tokens
    private static readonly Dictionary<TokenType, int> TokenMap = new()
    {
        { TokenType.tkIntegerLiteral, (int)Tokens.tkIntegerLiteral },
        { TokenType.tkRealLiteral, (int)Tokens.tkRealLiteral },
        { TokenType.tkBoolLiteral, (int)Tokens.tkBoolLiteral },
        { TokenType.tkIdentifier, (int)Tokens.tkIdentifier },
        { TokenType.tkVar, (int)Tokens.tkVar },
        { TokenType.tkRealKeyword, (int)Tokens.tkRealKeyword },
        { TokenType.tkBoolKeyword, (int)Tokens.tkBoolKeyword },
        { TokenType.tkIntegerKeyword, (int)Tokens.tkIntegerKeyword },
        { TokenType.tkType, (int)Tokens.tkType },
        { TokenType.tkIs, (int)Tokens.tkIs },
        { TokenType.tkIf, (int)Tokens.tkIf },
        { TokenType.tkThen, (int)Tokens.tkThen },
        { TokenType.tkElse, (int)Tokens.tkElse },
        { TokenType.tkWhile, (int)Tokens.tkWhile },
        { TokenType.tkFor, (int)Tokens.tkFor },
        { TokenType.tkIn, (int)Tokens.tkIn },
        { TokenType.tkRange, (int)Tokens.tkRange },
        { TokenType.tkLoop, (int)Tokens.tkLoop },
        { TokenType.tkReverse, (int)Tokens.tkReverse },
        { TokenType.tkRecord, (int)Tokens.tkRecord },
        { TokenType.tkRecordAccess, (int)Tokens.tkRecordAccess },
        { TokenType.tkRoutine, (int)Tokens.tkRoutine },
        { TokenType.tkReturn, (int)Tokens.tkReturn },
        { TokenType.tkArray, (int)Tokens.tkArray },
        { TokenType.tkEnd, (int)Tokens.tkEnd },
        { TokenType.tkPrint, (int)Tokens.tkPrint },
        { TokenType.tkPlus, (int)Tokens.tkPlus },
        { TokenType.tkMinus, (int)Tokens.tkMinus },
        { TokenType.tkMultiply, (int)Tokens.tkMultiply },
        { TokenType.tkDivide, (int)Tokens.tkDivide },
        { TokenType.tkMod, (int)Tokens.tkMod },
        { TokenType.tkAssign, (int)Tokens.tkAssign },
        { TokenType.tkLessThan, (int)Tokens.tkLessThan },
        { TokenType.tkLessThanOrEqual, (int)Tokens.tkLessThanOrEqual },
        { TokenType.tkGreaterThan, (int)Tokens.tkGreaterThan },
        { TokenType.tkGreaterThanOrEqual, (int)Tokens.tkGreaterThanOrEqual },
        { TokenType.tkEqual, (int)Tokens.tkEqual },
        { TokenType.tkNotEqual, (int)Tokens.tkNotEqual },
        { TokenType.tkAnd, (int)Tokens.tkAnd },
        { TokenType.tkOr, (int)Tokens.tkOr },
        { TokenType.tkNot, (int)Tokens.tkNot },
        { TokenType.tkXor, (int)Tokens.tkXor },
        { TokenType.tkDot, (int)Tokens.tkDot },
        { TokenType.tkColon, (int)Tokens.tkColon },
        { TokenType.tkComma, (int)Tokens.tkComma },
        { TokenType.tkLeftParen, (int)Tokens.tkLeftParen },
        { TokenType.tkRightParen, (int)Tokens.tkRightParen },
        { TokenType.tkLeftBracket, (int)Tokens.tkLeftBracket },
        { TokenType.tkRightBracket, (int)Tokens.tkRightBracket },
        { TokenType.tkEOF, (int)Tokens.EOF },  // Map tkEOF to EOF
        { TokenType.tkInvalid, (int)Tokens.tkInvalid }
    };

    public LexerAdapter(LexerClass lexer)
    {
        _lexer = lexer;
        _currentToken = null!;
    }

    /// <summary>
    /// Method called by parser to get next token
    /// </summary>
    public override int yylex()
    {
        _currentToken = _lexer.NextToken();
        
        // Set position for parser
        yylloc = new Compiler.Parser.LexLocation(
            _currentToken.Span.Line,
            _currentToken.Span.Start,
            _currentToken.Span.Line,
            _currentToken.Span.End
        );

        // Skip EOL tokens (not critical for parsing)
        if (_currentToken.Type == TokenType.tkEOL)
        {
            return yylex(); // Recursively get next token
        }

        // Save token value for use in semantic actions
        yylval = GetTokenValue(_currentToken);

        // Return mapped token value for parser
        if (TokenMap.TryGetValue(_currentToken.Type, out int parserToken))
        {
            return parserToken;
        }
        
        // Unknown token - return error
        return (int)Tokens.error;
    }

    /// <summary>
    /// Extract token value depending on its type
    /// </summary>
    private object GetTokenValue(Token token)
    {
        return token switch
        {
            IntegerToken intToken => intToken.Value,
            RealToken realToken => realToken.Value,
            BooleanToken boolToken => boolToken.Value,
            IdentifierToken idToken => idToken.Name,
            RecordAccessToken recordToken => recordToken.Lexeme,
            _ => token.Lexeme
        };
    }

    /// <summary>
    /// Get current token (for debugging and error messages)
    /// </summary>
    public Token CurrentToken => _currentToken;

    /// <summary>
    /// Parse error handling
    /// </summary>
    public override void yyerror(string format, params object[] args)
    {
        var message = string.Format(format, args);
        var location = yylloc != null 
            ? $" at line {yylloc.StartLine}, column {yylloc.StartColumn}" 
            : "";
        throw new ParseException($"Parse error{location}: {message}");
    }
}

/// <summary>
/// Parse exception
/// </summary>
public class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}
