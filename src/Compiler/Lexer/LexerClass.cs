using System.Globalization;

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
        if (_position > _source.Length - 2)
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

    private bool IsValidStartChar()
    {
        // Check if current character can start a valid token
        return char.IsDigit(_currentChar) 
            || char.IsLetter(_currentChar) 
            || char.IsWhiteSpace(_currentChar)
            || _currentChar == '\0'
            || TokenDefinitions.Operators.ContainsKey(_currentChar.ToString());
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

        // Check if number is followed by letter or underscore (invalid: 4fa, 123abc, etc.)
        if (char.IsLetter(_currentChar) || _currentChar == '_')
        {
            while (_currentChar != '\0' && (char.IsLetterOrDigit(_currentChar) || _currentChar == '_'))
            {
                lexeme += _currentChar;
                Move();
            }
            Span invalidSpan = new(startLine, startColumn, _column - 1);
            return new SimpleToken(TokenType.tkInvalid, lexeme, invalidSpan);
        }

        Span span = new(startLine, startColumn, _column - 1);

        if (hasError)
        {
            return new SimpleToken(TokenType.tkInvalid, lexeme, span);
        }

        if (isReal)
        {
            return double.TryParse(lexeme, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? new RealToken(lexeme, value, span)
                : new SimpleToken(TokenType.tkInvalid, lexeme, span);
        }
        else
        {
            return long.TryParse(lexeme, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
                ? new IntegerToken(lexeme, value, span)
                : new SimpleToken(TokenType.tkInvalid, lexeme, span);
        }
    }

    private Token ParseIdentifierOrKeyword(int startLine, int startColumn)
    {
        string lexeme = "";
        bool hasDot = false;
        bool isInvalid = false;

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

        // Check if we hit an invalid character (not whitespace, not EOL, not valid token start)
        // If so, consume everything until whitespace/EOL as one invalid token
        if (_currentChar != '\0' && !char.IsWhiteSpace(_currentChar) && !IsValidStartChar())
        {
            isInvalid = true;
            while (_currentChar != '\0' && !char.IsWhiteSpace(_currentChar))
            {
                lexeme += _currentChar;
                Move();
            }
        }

        Span span = new(startLine, startColumn, _column - 1);

        // If we encountered invalid characters, return invalid token
        if (isInvalid)
        {
            return new SimpleToken(TokenType.tkInvalid, lexeme, span);
        }

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
        string operatorLexeme = _currentChar.ToString();
        if ((LookAhead() == '=') || (LookAhead() == '.'))
        {
            operatorLexeme += LookAhead().ToString();
            if (TokenDefinitions.Operators.TryGetValue(operatorLexeme, out TokenType operType))
            {
                Span operatorSpan = new(startLine, startColumn, _column + 1);
                Move();
                Move();
                return new SimpleToken(operType, operatorLexeme, operatorSpan);
            }
        }
        if (TokenDefinitions.Operators.TryGetValue(_currentChar.ToString(), out TokenType operatorType))
        {
            Span operatorSpan = new(startLine, startColumn, _column);
            Move();
            return new SimpleToken(operatorType, operatorLexeme, operatorSpan);
        }

        Span span = new(startLine, startColumn, _column);
        Move();
        return new SimpleToken(TokenType.tkInvalid, _currentChar.ToString(), span);
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
                return new SimpleToken(TokenType.tkEOL, "\\n", new Span(_line - 1, oldColumn, oldColumn));
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
            if (_currentChar == '#')
            {
                // Skip comment until end of line
                while (_currentChar != '\0' && _currentChar != '\n')
                {
                    Move();
                }
                continue;
            }

            // Collect all consecutive invalid characters into one token
            int startLine = _line;
            int startColumn = _column;
            string invalidLexeme = "";
            
            while (_currentChar != '\0' && !IsValidStartChar())
            {
                invalidLexeme += _currentChar;
                Move();
            }

            Span span = new(startLine, startColumn, _column - 1);
            return new SimpleToken(TokenType.tkInvalid, invalidLexeme, span);
        }
        return new SimpleToken(TokenType.tkEOF, "", new Span(_line, _column, _column));
    }
}