namespace Compiler.Lexer;

public enum TokenType
{
    // Литералы
    tkIntegerLiteral,
    tkRealLiteral,
    tkBoolLiteral,
    // String,
    tkIdentifier,

    // Ключевые слова
    tkVar,
    tkRealKeyword,
    tkBoolKeyword,
    tkIntegerKeyword,
    tkType,
    tkIs,
    tkIf,
    tkThen,
    tkElse,
    tkWhile,
    tkFor,
    tkIn,
    tkLoop,
    tkReverse,
    tkRecord,
    tkRoutine,
    tkReturn,
    tkArray,
    tkEnd,
    tkPrint,

    // Операторы
    tkPlus,
    tkMinus,
    tkMultiply,
    tkDivide,
    tkMod,
    tkAssign,
    tkLessThan,
    tkLessThanOrEqual,
    tkGreaterThan,
    tkGreaterThanOrEqual,
    tkEqual,
    tkNotEqual,
    tkAnd,
    tkOr,
    tkNot,
    tkXor,

    // Символы
    tkDot,
    tkColon,
    tkComma,
    tkLeftParen,
    tkRightParen,
    tkLeftBracket,
    tkRightBracket,
    // Semicolon,

    // Специальные
    tkWhitespace,
    tkEOL,
    tkEOF,
    tkInvalid
}
