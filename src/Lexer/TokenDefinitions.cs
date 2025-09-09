namespace Compiler.Lexer;

public static class TokenDefinitions
{
    public static readonly Dictionary<string, TokenType> Keywords = new()
    {
        {"var", TokenType.tkVar},
        {"real", TokenType.tkRealKeyword},
        {"boolean", TokenType.tkBoolKeyword},
        {"integer", TokenType.tkIntegerKeyword},
        {"type", TokenType.tkType},
        {"is", TokenType.tkIs},
        {"if", TokenType.tkIf},
        {"then", TokenType.tkThen},
        {"else", TokenType.tkElse},
        {"while", TokenType.tkWhile},
        {"for", TokenType.tkFor},
        {"in", TokenType.tkIn},
        {"loop", TokenType.tkLoop},
        {"reverse", TokenType.tkReverse},
        {"record", TokenType.tkRecord},
        {"routine", TokenType.tkRoutine},
        {"return", TokenType.tkReturn},
        {"array", TokenType.tkArray},
        {"end", TokenType.tkEnd},
        {"print", TokenType.tkPrint},
        {"true", TokenType.tkBoolLiteral},
        {"false", TokenType.tkBoolLiteral},
        {"and", TokenType.tkAnd},
        {"or", TokenType.tkOr},
        {"not", TokenType.tkNot},
        {"xor", TokenType.tkXor}
    };

    public static readonly Dictionary<string, TokenType> Operators = new()
    {
        {"+", TokenType.tkPlus},
        {"-", TokenType.tkMinus},
        {"*", TokenType.tkMultiply},
        {"/", TokenType.tkDivide},
        {"%", TokenType.tkMod},
        {":=", TokenType.tkAssign},
        {"<", TokenType.tkLessThan},
        {"<=", TokenType.tkLessThanOrEqual},
        {">", TokenType.tkGreaterThan},
        {">=", TokenType.tkGreaterThanOrEqual},
        {"=", TokenType.tkEqual},
        {"/=", TokenType.tkNotEqual},
        {".", TokenType.tkDot},
        {":", TokenType.tkColon},
        {",", TokenType.tkComma},
        {"(", TokenType.tkLeftParen},
        {")", TokenType.tkRightParen},
        {"[", TokenType.tkLeftBracket},
        {"]", TokenType.tkRightBracket}
    };
}