namespace Compiler.Lexer;

public class LexerClass
{
    private readonly string _source;
    private int _position;
    private int _line;
    private int _column;
    private char _currentChar;

    public LexerClass(string source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _currentChar = _source.Length > 0 ? _source[0] : '\0';
    }

    private char LookAhead()
    {
        if (_position + 1 > _source.Length)
        {
            return '\0';
        }
        return _source[_position + 1];
    }

    private void Move()
    {
        if (_currentChar == '\n')
        {
            _line++;
            _column = 0;
        }
        _position++;
        _column++;
        if (_position >= _source.Length)
        {
            _currentChar = '\0';
        }
        else
        {
            _currentChar = _source[_position];
        }
    }

    private void SkipWhitespace()
    {
        while (_currentChar != '\0' && char.IsWhiteSpace(_currentChar))
        {
            Move();
        }
    }

    private Token ParseNumber(int startLine, int startColumn)
    {
        string lexeme = "";
        bool isReal = false;
        bool hasError = false;

        while (_currentChar != '\0' && (char.IsDigit(_currentChar) || _currentChar == '.'))
        {
            if (_currentChar == '.')
            {
                if (LookAhead() == '.') break; // range

                if (isReal) hasError = true;

                isReal = true;
            }
            lexeme += _currentChar;
            Move();
        }

        Span span = new(startLine, startColumn, _column - 1);

        if (hasError)
        {
            return new SimpleToken(TokenType.tkInvalid, lexeme, span);
        }

        if (isReal)
        {
            return double.TryParse(lexeme, out double value)
                ? new RealToken(lexeme, value, span)
                : new SimpleToken(TokenType.tkInvalid, lexeme, span);
        }
        else
        {
            return long.TryParse(lexeme, out long value)
                ? new IntegerToken(lexeme, value, span) 
                : new SimpleToken(TokenType.tkInvalid, lexeme, span);
        }
    }

    private Token ParseIdentifierOrKeyword(int startLine, int startColumn)
    {
        string lexeme = "";
        bool hasDot = false;

        while (_currentChar != '\0' && (char.IsLetterOrDigit(_currentChar) || _currentChar == '_'))
        {
            lexeme += _currentChar;
            Move();
        }

        while (_currentChar == '.')
        {
            if (LookAhead() == '.') break; // range
            lexeme += _currentChar;
            Move();
            while (_currentChar != '\0' && (char.IsLetterOrDigit(_currentChar) || _currentChar == '_'))
            {
                lexeme += _currentChar;
                Move();
            }
            hasDot = true;
        }

        Span span = new(startLine, startColumn, _column - 1);

        if (hasDot)
        {
            return new RecordAccessToken(lexeme, span);
        }

        if (TokenDefinitions.Keywords.TryGetValue(lexeme, out TokenType keywordType))
        {
            if (keywordType == TokenType.tkBoolLiteral)
            {
                bool value = lexeme == "true";
                return new BooleanToken(lexeme, value, span);
            }
            else
            {
                return new SimpleToken(keywordType, lexeme, span);
            }
        }
        else
        {
            return new IdentifierToken(lexeme, span);
        }
    }
    private Token ReadOperator(int startLine, int startColumn)
    {
        if (_currentChar == '.' && LookAhead() == '.')
        {
            Move();
            Move();
            Span rangeSpan = new(startLine, startColumn, _column - 1);
            return new SimpleToken(TokenType.tkRange, "..", rangeSpan);
        }

        if (_currentChar == ':' && LookAhead() == '=')
        {
            Move();
            Move();
            Span assignRange = new(startLine, startColumn, _column - 1);
            return new SimpleToken(TokenType.tkAssign, ":=", assignRange);
        }

        if (_currentChar == '/' && LookAhead() == '=')
        {
            Move();
            Move();
            Span notEqualSpan = new(startLine, startColumn, _column - 1);
            return new SimpleToken(TokenType.tkNotEqual, "/=", notEqualSpan);
        }

        if (_currentChar == '<' && LookAhead() == '=')
        {
            Move();
            Move();
            Span lessOrEqualSpan = new(startLine, startColumn, _column - 1);
            return new SimpleToken(TokenType.tkLessThanOrEqual, "<=", lessOrEqualSpan);
        }

        if (_currentChar == '>' && LookAhead() == '=')
        {
            Move();
            Move();
            Span greaterOrEqualSpan = new(startLine, startColumn, _column - 1);
            return new SimpleToken(TokenType.tkGreaterThanOrEqual, ">=", greaterOrEqualSpan);
        }

        string operatorLexeme = _currentChar.ToString();
        if (TokenDefinitions.Operators.TryGetValue(operatorLexeme, out TokenType operatorType))
        {
            Span operatorSpan = new(startLine, startColumn, _column);
            Move();
            return new SimpleToken(operatorType, operatorLexeme, operatorSpan);
        }

        Span span = new(startLine, startColumn, _column);
        Move();
        return new SimpleToken(TokenType.tkInvalid, operatorLexeme, span);
    }
    public Token NextToken()
    {
        while (_currentChar != '\0')
        {
            if (_currentChar == '\n')
            {
                int oldColumn = _column;
                _column = 1;
                Move();
                return new SimpleToken(TokenType.tkEOL, "\\n", new Span(_line++, oldColumn, oldColumn));
            }
            if (char.IsWhiteSpace(_currentChar))
            {
                SkipWhitespace();
                continue;
            }

            if (char.IsDigit(_currentChar))
            {
                return ParseNumber(_line, _column);
            }

            if (char.IsLetter(_currentChar))
            {
                return ParseIdentifierOrKeyword(_line, _column);
            }

            if (TokenDefinitions.Operators.ContainsKey(_currentChar.ToString()))
            {
                return ReadOperator(_line, _column);
            }

            Span span = new(_line, _column, _column);
            string invalidLexeme = _currentChar.ToString();
            Move();
            return new SimpleToken(TokenType.tkInvalid, invalidLexeme, span);
        }
        return new SimpleToken(TokenType.tkEOF, "", new Span(_line, _column, _column));
    }
}