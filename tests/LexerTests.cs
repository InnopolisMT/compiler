using Xunit;
using Compiler.Lexer;
using System.Collections.Generic;
using System.Linq;

namespace Compiler.Tests
{
    public class LexerTests
    {
        private List<Token> TokenizeFile(string fileName)
        {
            string filePath = Path.Combine("test_files", fileName);
            string content = File.ReadAllText(filePath);
            
            var lexer = new LexerClass(content);
            var tokens = new List<Token>();
            
            Token token;
            do
            {
                token = lexer.NextToken();
                tokens.Add(token);
            } while (token.Type != TokenType.tkEOF);
            
            return tokens;
        }

        private void AssertToken(Token token, TokenType expectedType, string expectedLexeme, int expectedLine, int expectedStart, int expectedEnd)
        {
            Assert.Equal(expectedType, token.Type);
            Assert.Equal(expectedLexeme, token.Lexeme);
            Assert.Equal(expectedLine, token.Span.Line);
            Assert.Equal(expectedStart, token.Span.Start);
            Assert.Equal(expectedEnd, token.Span.End);
        }

        private void AssertIntegerToken(IntegerToken token, long expectedValue, string expectedLexeme, int expectedLine, int expectedStart, int expectedEnd)
        {
            Assert.Equal(TokenType.tkIntegerLiteral, token.Type);
            Assert.Equal(expectedValue, token.Value);
            Assert.Equal(expectedLexeme, token.Lexeme);
            Assert.Equal(expectedLine, token.Span.Line);
            Assert.Equal(expectedStart, token.Span.Start);
            Assert.Equal(expectedEnd, token.Span.End);
        }

        private void AssertRealToken(RealToken token, double expectedValue, string expectedLexeme, int expectedLine, int expectedStart, int expectedEnd)
        {
            Assert.Equal(TokenType.tkRealLiteral, token.Type);
            Assert.Equal(expectedValue, token.Value);
            Assert.Equal(expectedLexeme, token.Lexeme);
            Assert.Equal(expectedLine, token.Span.Line);
            Assert.Equal(expectedStart, token.Span.Start);
            Assert.Equal(expectedEnd, token.Span.End);
        }

        private void AssertBooleanToken(BooleanToken token, bool expectedValue, string expectedLexeme, int expectedLine, int expectedStart, int expectedEnd)
        {
            Assert.Equal(TokenType.tkBoolLiteral, token.Type);
            Assert.Equal(expectedValue, token.Value);
            Assert.Equal(expectedLexeme, token.Lexeme);
            Assert.Equal(expectedLine, token.Span.Line);
            Assert.Equal(expectedStart, token.Span.Start);
            Assert.Equal(expectedEnd, token.Span.End);
        }

        private void AssertRecordAccessToken(RecordAccessToken token, string expectedLexeme, int expectedLine, int expectedStart, int expectedEnd)
        {
            Assert.Equal(TokenType.tkRecordAccess, token.Type);
            Assert.Equal(expectedLexeme, token.Lexeme);
            Assert.Equal(expectedLine, token.Span.Line);
            Assert.Equal(expectedStart, token.Span.Start);
            Assert.Equal(expectedEnd, token.Span.End);
        }

        [Fact]
        public void TestBasicIdentifiers()
        {
            var tokens = TokenizeFile("basic_identifiers.txt");
            
            Assert.Equal(3, tokens.Count); // identifier + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkIdentifier, "variable", 1, 1, 8);
            AssertToken(tokens[1], TokenType.tkEOL, "\\n", 1, 9, 9);
            AssertToken(tokens[2], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestIntegerLiterals()
        {
            var tokens = TokenizeFile("integer_literals.txt");
            
            Assert.Equal(5, tokens.Count); // 3 integers + EOL + EOF
            
            AssertIntegerToken((IntegerToken)tokens[0], 123, "123", 1, 1, 3);
            AssertIntegerToken((IntegerToken)tokens[1], 0, "0", 1, 5, 5);
            AssertIntegerToken((IntegerToken)tokens[2], 999999, "999999", 1, 7, 12);
            AssertToken(tokens[3], TokenType.tkEOL, "\\n", 1, 13, 13);
            AssertToken(tokens[4], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestRealLiterals()
        {
            var tokens = TokenizeFile("real_literals.txt");
            
            Assert.Equal(5, tokens.Count); // 3 reals + EOL + EOF
            
            AssertRealToken((RealToken)tokens[0], 123.45, "123.45", 1, 1, 6);
            AssertRealToken((RealToken)tokens[1], 0.0, "0.0", 1, 8, 10);
            AssertRealToken((RealToken)tokens[2], 999.999, "999.999", 1, 12, 18);
            AssertToken(tokens[3], TokenType.tkEOL, "\\n", 1, 19, 19);
            AssertToken(tokens[4], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestBooleanLiterals()
        {
            var tokens = TokenizeFile("boolean_literals.txt");
            
            Assert.Equal(4, tokens.Count); // 2 booleans + EOL + EOF
            
            AssertBooleanToken((BooleanToken)tokens[0], true, "true", 1, 1, 4);
            AssertBooleanToken((BooleanToken)tokens[1], false, "false", 1, 6, 10);
            AssertToken(tokens[2], TokenType.tkEOL, "\\n", 1, 11, 11);
            AssertToken(tokens[3], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestKeywords()
        {
            var tokens = TokenizeFile("keywords.txt");
            
            Assert.Equal(12, tokens.Count); // 10 keywords + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkVar, "var", 1, 1, 3);
            AssertToken(tokens[1], TokenType.tkType, "type", 1, 5, 8);
            AssertToken(tokens[2], TokenType.tkIs, "is", 1, 10, 11);
            AssertToken(tokens[3], TokenType.tkRecord, "record", 1, 13, 18);
            AssertToken(tokens[4], TokenType.tkRoutine, "routine", 1, 20, 26);
            AssertToken(tokens[5], TokenType.tkIf, "if", 1, 28, 29);
            AssertToken(tokens[6], TokenType.tkThen, "then", 1, 31, 34);
            AssertToken(tokens[7], TokenType.tkFor, "for", 1, 36, 38);
            AssertToken(tokens[8], TokenType.tkIn, "in", 1, 40, 41);
            AssertToken(tokens[9], TokenType.tkLoop, "loop", 1, 43, 46);
            AssertToken(tokens[10], TokenType.tkEOL, "\\n", 1, 47, 47);
            AssertToken(tokens[11], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestOperators()
        {
            var tokens = TokenizeFile("operators.txt");
            
            Assert.Equal(15, tokens.Count); // 13 operators + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkPlus, "+", 1, 1, 1);
            AssertToken(tokens[1], TokenType.tkMinus, "-", 1, 3, 3);
            AssertToken(tokens[2], TokenType.tkMultiply, "*", 1, 5, 5);
            AssertToken(tokens[3], TokenType.tkDivide, "/", 1, 7, 7);
            AssertToken(tokens[4], TokenType.tkAssign, ":=", 1, 9, 10);
            AssertToken(tokens[5], TokenType.tkEqual, "=", 1, 12, 12);
            AssertToken(tokens[6], TokenType.tkNotEqual, "/=", 1, 14, 15);
            AssertToken(tokens[7], TokenType.tkLessThan, "<", 1, 17, 17);
            AssertToken(tokens[8], TokenType.tkLessThanOrEqual, "<=", 1, 19, 20);
            AssertToken(tokens[9], TokenType.tkGreaterThan, ">", 1, 22, 22);
            AssertToken(tokens[10], TokenType.tkGreaterThanOrEqual, ">=", 1, 24, 25);
            AssertToken(tokens[11], TokenType.tkRange, "..", 1, 27, 28);
            AssertToken(tokens[12], TokenType.tkDot, ".", 1, 30, 30);
            AssertToken(tokens[13], TokenType.tkEOL, "\\n", 1, 31, 31);
            AssertToken(tokens[14], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestRecordAccess()
        {
            var tokens = TokenizeFile("record_access.txt");
            
            Assert.Equal(5, tokens.Count); // 3 record access + EOL + EOF
            
            AssertRecordAccessToken((RecordAccessToken)tokens[0], "person.name", 1, 1, 11);
            AssertRecordAccessToken((RecordAccessToken)tokens[1], "person.age", 1, 13, 22);
            AssertRecordAccessToken((RecordAccessToken)tokens[2], "person.addr.streetCode", 1, 24, 45);
            AssertToken(tokens[3], TokenType.tkEOL, "\\n", 1, 46, 46);
            AssertToken(tokens[4], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestTypeDeclarations()
        {
            var tokens = TokenizeFile("type_declarations.txt");
            
            Assert.Equal(13, tokens.Count); // type declaration tokens + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkType, "type", 1, 1, 4);
            AssertToken(tokens[1], TokenType.tkIdentifier, "Person", 1, 6, 11);
            AssertToken(tokens[2], TokenType.tkIs, "is", 1, 13, 14);
            AssertToken(tokens[3], TokenType.tkRecord, "record", 1, 16, 21);
            AssertToken(tokens[4], TokenType.tkEOL, "\\n", 1, 22, 22);
            AssertToken(tokens[5], TokenType.tkVar, "var", 2, 5, 7);
            AssertToken(tokens[6], TokenType.tkIdentifier, "age", 2, 9, 11);
            AssertToken(tokens[7], TokenType.tkColon, ":", 2, 13, 13);
            AssertToken(tokens[8], TokenType.tkIntegerKeyword, "integer", 2, 15, 21);
            AssertToken(tokens[9], TokenType.tkEOL, "\\n", 2, 22, 22);
            AssertToken(tokens[10], TokenType.tkEnd, "end", 3, 1, 3);
            AssertToken(tokens[11], TokenType.tkEOL, "\\n", 3, 4, 4);
            AssertToken(tokens[12], TokenType.tkEOF, "", 4, 1, 1);
        }

        [Fact]
        public void TestArrayDeclarations()
        {
            var tokens = TokenizeFile("array_declarations.txt");
            
            Assert.Equal(10, tokens.Count); // array declaration tokens + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkType, "type", 1, 1, 4);
            AssertToken(tokens[1], TokenType.tkIdentifier, "IntArray", 1, 6, 13);
            AssertToken(tokens[2], TokenType.tkIs, "is", 1, 15, 16);
            AssertToken(tokens[3], TokenType.tkArray, "array", 1, 18, 22);
            AssertToken(tokens[4], TokenType.tkLeftBracket, "[", 1, 23, 23);
            AssertIntegerToken((IntegerToken)tokens[5], 5, "5", 1, 24, 24);
            AssertToken(tokens[6], TokenType.tkRightBracket, "]", 1, 25, 25);
            AssertToken(tokens[7], TokenType.tkIntegerKeyword, "integer", 1, 27, 33);
            AssertToken(tokens[8], TokenType.tkEOL, "\\n", 1, 34, 34);
            AssertToken(tokens[9], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestControlStructures()
        {
            var tokens = TokenizeFile("control_structures.txt");
            
            Assert.Equal(25, tokens.Count); // control structure tokens + EOL + EOF
            
            // if statement
            AssertToken(tokens[0], TokenType.tkIf, "if", 1, 1, 2);
            AssertToken(tokens[1], TokenType.tkLeftParen, "(", 1, 4, 4);
            AssertToken(tokens[2], TokenType.tkIdentifier, "x", 1, 5, 5);
            AssertToken(tokens[3], TokenType.tkEqual, "=", 1, 7, 7);
            AssertIntegerToken((IntegerToken)tokens[4], 5, "5", 1, 9, 9);
            AssertToken(tokens[5], TokenType.tkRightParen, ")", 1, 10, 10);
            AssertToken(tokens[6], TokenType.tkThen, "then", 1, 12, 15);
            AssertToken(tokens[7], TokenType.tkEOL, "\\n", 1, 16, 16);
            AssertToken(tokens[8], TokenType.tkPrint, "print", 2, 5, 9);
            AssertIntegerToken((IntegerToken)tokens[9], 5, "5", 2, 11, 11);
            AssertToken(tokens[10], TokenType.tkEOL, "\\n", 2, 12, 12);
            AssertToken(tokens[11], TokenType.tkEnd, "end", 3, 1, 3);
            AssertToken(tokens[12], TokenType.tkEOL, "\\n", 3, 4, 4);
            AssertToken(tokens[13], TokenType.tkEOL, "\\n", 4, 1, 1);
            
            // for loop
            AssertToken(tokens[14], TokenType.tkFor, "for", 5, 1, 3);
            AssertToken(tokens[15], TokenType.tkIdentifier, "i", 5, 5, 5);
            AssertToken(tokens[16], TokenType.tkIn, "in", 5, 7, 8);
            AssertIntegerToken((IntegerToken)tokens[17], 1, "1", 5, 10, 10);
            AssertToken(tokens[18], TokenType.tkRange, "..", 5, 12, 13);
            AssertIntegerToken((IntegerToken)tokens[19], 5, "5", 5, 15, 15);
            AssertToken(tokens[20], TokenType.tkLoop, "loop", 5, 17, 20);
            AssertToken(tokens[21], TokenType.tkEOL, "\\n", 5, 21, 21);
            AssertToken(tokens[22], TokenType.tkEnd, "end", 6, 1, 3);
            AssertToken(tokens[23], TokenType.tkEOL, "\\n", 6, 4, 4);
            AssertToken(tokens[24], TokenType.tkEOF, "", 7, 1, 1);
        }

        [Fact]
        public void TestComplexCases()
        {
            var tokens = TokenizeFile("complex_cases.txt");
            
            Assert.Equal(14, tokens.Count); // complex case tokens + EOL + EOF
            
            // Range with record access
            AssertToken(tokens[0], TokenType.tkFor, "for", 1, 1, 3);
            AssertToken(tokens[1], TokenType.tkIdentifier, "i", 1, 5, 5);
            AssertToken(tokens[2], TokenType.tkIn, "in", 1, 7, 8);
            AssertRecordAccessToken((RecordAccessToken)tokens[3], "person.age", 1, 10, 19);
            AssertToken(tokens[4], TokenType.tkRange, "..", 1, 20, 21);
            AssertIntegerToken((IntegerToken)tokens[5], 50, "50", 1, 22, 23);
            AssertToken(tokens[6], TokenType.tkLoop, "loop", 1, 25, 28);
            AssertToken(tokens[7], TokenType.tkEOL, "\\n", 1, 29, 29);
            AssertToken(tokens[8], TokenType.tkPrint, "print", 2, 5, 9);
            AssertToken(tokens[9], TokenType.tkIdentifier, "i", 2, 11, 11);
            AssertToken(tokens[10], TokenType.tkEOL, "\\n", 2, 12, 12);
            AssertToken(tokens[11], TokenType.tkEnd, "end", 3, 1, 3);
            AssertToken(tokens[12], TokenType.tkEOL, "\\n", 3, 4, 4);
            AssertToken(tokens[13], TokenType.tkEOF, "", 4, 1, 1);
        }

        [Fact]
        public void TestErrorCases()
        {
            var tokens = TokenizeFile("error_cases.txt");
            
            Assert.Equal(5, tokens.Count); // error tokens + EOL + EOF
            
            // Invalid number with multiple dots
            AssertToken(tokens[0], TokenType.tkInvalid, "123.45.67", 1, 1, 9);
            AssertToken(tokens[1], TokenType.tkEOL, "\\n", 1, 10, 10);
            
            // Invalid character
            AssertToken(tokens[2], TokenType.tkInvalid, "@", 2, 1, 1);
            AssertToken(tokens[3], TokenType.tkEOL, "\\n", 2, 2, 2);
            AssertToken(tokens[4], TokenType.tkEOF, "", 3, 1, 1);
        }

        [Fact]
        public void TestAdvancedOperators()
        {
            var tokens = TokenizeFile("advanced_operators.txt");
            
            Assert.Equal(7, tokens.Count); // 5 operators + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkMod, "%", 1, 1, 1);
            AssertToken(tokens[1], TokenType.tkAnd, "and", 1, 3, 5);
            AssertToken(tokens[2], TokenType.tkOr, "or", 1, 7, 8);
            AssertToken(tokens[3], TokenType.tkNot, "not", 1, 10, 12);
            AssertToken(tokens[4], TokenType.tkXor, "xor", 1, 14, 16);
            AssertToken(tokens[5], TokenType.tkEOL, "\\n", 1, 17, 17);
            AssertToken(tokens[6], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestDelimiters()
        {
            var tokens = TokenizeFile("delimiters.txt");
            
            Assert.Equal(8, tokens.Count); // 6 delimiters + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkLeftParen, "(", 1, 1, 1);
            AssertToken(tokens[1], TokenType.tkRightParen, ")", 1, 3, 3);
            AssertToken(tokens[2], TokenType.tkLeftBracket, "[", 1, 5, 5);
            AssertToken(tokens[3], TokenType.tkRightBracket, "]", 1, 7, 7);
            AssertToken(tokens[4], TokenType.tkComma, ",", 1, 9, 9);
            AssertToken(tokens[5], TokenType.tkColon, ":", 1, 11, 11);
            AssertToken(tokens[6], TokenType.tkEOL, "\\n", 1, 12, 12);
            AssertToken(tokens[7], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestArrayInitialization()
        {
            var tokens = TokenizeFile("array_initialization.txt");
            
            Assert.Equal(18, tokens.Count); // array initialization tokens + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkVar, "var", 1, 1, 3);
            AssertToken(tokens[1], TokenType.tkIdentifier, "arr", 1, 5, 7);
            AssertToken(tokens[2], TokenType.tkColon, ":", 1, 9, 9);
            AssertToken(tokens[3], TokenType.tkIdentifier, "IntArray", 1, 11, 18);
            AssertToken(tokens[4], TokenType.tkIs, "is", 1, 20, 21);
            AssertToken(tokens[5], TokenType.tkLeftBracket, "[", 1, 23, 23);
            AssertIntegerToken((IntegerToken)tokens[6], 1, "1", 1, 24, 24);
            AssertToken(tokens[7], TokenType.tkComma, ",", 1, 25, 25);
            AssertIntegerToken((IntegerToken)tokens[8], 2, "2", 1, 27, 27);
            AssertToken(tokens[9], TokenType.tkComma, ",", 1, 28, 28);
            AssertIntegerToken((IntegerToken)tokens[10], 3, "3", 1, 30, 30);
            AssertToken(tokens[11], TokenType.tkComma, ",", 1, 31, 31);
            AssertIntegerToken((IntegerToken)tokens[12], 4, "4", 1, 33, 33);
            AssertToken(tokens[13], TokenType.tkComma, ",", 1, 34, 34);
            AssertIntegerToken((IntegerToken)tokens[14], 5, "5", 1, 36, 36);
            AssertToken(tokens[15], TokenType.tkRightBracket, "]", 1, 37, 37);
            AssertToken(tokens[16], TokenType.tkEOL, "\\n", 1, 38, 38);
            AssertToken(tokens[17], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestNestedRecordAccess()
        {
            var tokens = TokenizeFile("nested_record_access.txt");
            
            Assert.Equal(4, tokens.Count); // 2 nested record access + EOL + EOF
            
            AssertRecordAccessToken((RecordAccessToken)tokens[0], "person.addr.streetCode", 1, 1, 22);
            AssertRecordAccessToken((RecordAccessToken)tokens[1], "person.addr.number", 1, 24, 41);
            AssertToken(tokens[2], TokenType.tkEOL, "\\n", 1, 42, 42);
            AssertToken(tokens[3], TokenType.tkEOF, "", 2, 1, 1);
        }

        [Fact]
        public void TestRangeWithExpressions()
        {
            var tokens = TokenizeFile("range_with_expressions.txt");
            
            Assert.Equal(17, tokens.Count); // range expressions + EOL + EOF
            
            // First for loop
            AssertToken(tokens[0], TokenType.tkFor, "for", 1, 1, 3);
            AssertToken(tokens[1], TokenType.tkIdentifier, "i", 1, 5, 5);
            AssertToken(tokens[2], TokenType.tkIn, "in", 1, 7, 8);
            AssertIntegerToken((IntegerToken)tokens[3], 1, "1", 1, 10, 10);
            AssertToken(tokens[4], TokenType.tkRange, "..", 1, 12, 13);
            AssertRecordAccessToken((RecordAccessToken)tokens[5], "person.age", 1, 15, 24);
            AssertToken(tokens[6], TokenType.tkLoop, "loop", 1, 26, 29);
            AssertToken(tokens[7], TokenType.tkEOL, "\\n", 1, 30, 30);
            
            // Second for loop
            AssertToken(tokens[8], TokenType.tkFor, "for", 2, 1, 3);
            AssertToken(tokens[9], TokenType.tkIdentifier, "j", 2, 5, 5);
            AssertToken(tokens[10], TokenType.tkIn, "in", 2, 7, 8);
            AssertRecordAccessToken((RecordAccessToken)tokens[11], "person.addr.streetCode", 2, 10, 31);
            AssertToken(tokens[12], TokenType.tkRange, "..", 2, 33, 34);
            AssertIntegerToken((IntegerToken)tokens[13], 100, "100", 2, 36, 38);
            AssertToken(tokens[14], TokenType.tkLoop, "loop", 2, 40, 43);
            AssertToken(tokens[15], TokenType.tkEOL, "\\n", 2, 44, 44);
            AssertToken(tokens[16], TokenType.tkEOF, "", 3, 1, 1);
        }

        [Fact]
        public void TestAssignmentStatements()
        {
            var tokens = TokenizeFile("assignment_statements.txt");
            
            Assert.Equal(16, tokens.Count); // assignment statements + EOL + EOF
            
            // x := 5
            AssertToken(tokens[0], TokenType.tkIdentifier, "x", 1, 1, 1);
            AssertToken(tokens[1], TokenType.tkAssign, ":=", 1, 3, 4);
            AssertIntegerToken((IntegerToken)tokens[2], 5, "5", 1, 6, 6);
            AssertToken(tokens[3], TokenType.tkEOL, "\\n", 1, 7, 7);
            
            AssertRecordAccessToken((RecordAccessToken)tokens[4], "person.age", 2, 1, 10);
            AssertToken(tokens[5], TokenType.tkAssign, ":=", 2, 12, 13);
            AssertIntegerToken((IntegerToken)tokens[6], 30, "30", 2, 15, 16);
            AssertToken(tokens[7], TokenType.tkEOL, "\\n", 2, 17, 17);
            
            // arr[0] := 10
            AssertToken(tokens[8], TokenType.tkIdentifier, "arr", 3, 1, 3);
            AssertToken(tokens[9], TokenType.tkLeftBracket, "[", 3, 4, 4);
            AssertIntegerToken((IntegerToken)tokens[10], 0, "0", 3, 5, 5);
            AssertToken(tokens[11], TokenType.tkRightBracket, "]", 3, 6, 6);
            AssertToken(tokens[12], TokenType.tkAssign, ":=", 3, 8, 9);
            AssertIntegerToken((IntegerToken)tokens[13], 10, "10", 3, 11, 12);
            AssertToken(tokens[14], TokenType.tkEOL, "\\n", 3, 13, 13);
            AssertToken(tokens[15], TokenType.tkEOF, "", 4, 1, 1);
        }

        [Fact]
        public void TestConditionalExpressions()
        {
            var tokens = TokenizeFile("conditional_expressions.txt");
            
            Assert.Equal(28, tokens.Count); // conditional expressions + EOL + EOF
            
            // if (x = 5) then
            AssertToken(tokens[0], TokenType.tkIf, "if", 1, 1, 2);
            AssertToken(tokens[1], TokenType.tkLeftParen, "(", 1, 4, 4);
            AssertToken(tokens[2], TokenType.tkIdentifier, "x", 1, 5, 5);
            AssertToken(tokens[3], TokenType.tkEqual, "=", 1, 7, 7);
            AssertIntegerToken((IntegerToken)tokens[4], 5, "5", 1, 9, 9);
            AssertToken(tokens[5], TokenType.tkRightParen, ")", 1, 10, 10);
            AssertToken(tokens[6], TokenType.tkThen, "then", 1, 12, 15);
            AssertToken(tokens[7], TokenType.tkEOL, "\\n", 1, 16, 16);
            
            // if (person.age > 18) then
            AssertToken(tokens[8], TokenType.tkIf, "if", 2, 1, 2);
            AssertToken(tokens[9], TokenType.tkLeftParen, "(", 2, 4, 4);
            AssertRecordAccessToken((RecordAccessToken)tokens[10], "person.age", 2, 5, 14);
            AssertToken(tokens[11], TokenType.tkGreaterThan, ">", 2, 16, 16);
            AssertIntegerToken((IntegerToken)tokens[12], 18, "18", 2, 18, 19);
            AssertToken(tokens[13], TokenType.tkRightParen, ")", 2, 20, 20);
            AssertToken(tokens[14], TokenType.tkThen, "then", 2, 22, 25);
            AssertToken(tokens[15], TokenType.tkEOL, "\\n", 2, 26, 26);
            
            // if (arr[0] <= 10) then
            AssertToken(tokens[16], TokenType.tkIf, "if", 3, 1, 2);
            AssertToken(tokens[17], TokenType.tkLeftParen, "(", 3, 4, 4);
            AssertToken(tokens[18], TokenType.tkIdentifier, "arr", 3, 5, 7);
            AssertToken(tokens[19], TokenType.tkLeftBracket, "[", 3, 8, 8);
            AssertIntegerToken((IntegerToken)tokens[20], 0, "0", 3, 9, 9);
            AssertToken(tokens[21], TokenType.tkRightBracket, "]", 3, 10, 10);
            AssertToken(tokens[22], TokenType.tkLessThanOrEqual, "<=", 3, 12, 13);
            AssertIntegerToken((IntegerToken)tokens[23], 10, "10", 3, 15, 16);
            AssertToken(tokens[24], TokenType.tkRightParen, ")", 3, 17, 17);
            AssertToken(tokens[25], TokenType.tkThen, "then", 3, 19, 22);
            AssertToken(tokens[26], TokenType.tkEOL, "\\n", 3, 23, 23);
            AssertToken(tokens[27], TokenType.tkEOF, "", 4, 1, 1);
        }

        [Fact]
        public void TestPrintStatements()
        {
            var tokens = TokenizeFile("print_statements.txt");
            
            Assert.Equal(13, tokens.Count); // print statements + EOL + EOF
            
            // print x
            AssertToken(tokens[0], TokenType.tkPrint, "print", 1, 1, 5);
            AssertToken(tokens[1], TokenType.tkIdentifier, "x", 1, 7, 7);
            AssertToken(tokens[2], TokenType.tkEOL, "\\n", 1, 8, 8);
            
            // print person.name
            AssertToken(tokens[3], TokenType.tkPrint, "print", 2, 1, 5);
            AssertRecordAccessToken((RecordAccessToken)tokens[4], "person.name", 2, 7, 17);
            AssertToken(tokens[5], TokenType.tkEOL, "\\n", 2, 18, 18);
            
            // print arr[0]
            AssertToken(tokens[6], TokenType.tkPrint, "print", 3, 1, 5);
            AssertToken(tokens[7], TokenType.tkIdentifier, "arr", 3, 7, 9);
            AssertToken(tokens[8], TokenType.tkLeftBracket, "[", 3, 10, 10);
            AssertIntegerToken((IntegerToken)tokens[9], 0, "0", 3, 11, 11);
            AssertToken(tokens[10], TokenType.tkRightBracket, "]", 3, 12, 12);
            AssertToken(tokens[11], TokenType.tkEOL, "\\n", 3, 13, 13);
            AssertToken(tokens[12], TokenType.tkEOF, "", 4, 1, 1);
        }

        [Fact]
        public void TestWhitespaceHandling()
        {
            var tokens = TokenizeFile("whitespace_handling.txt");
            
            Assert.Equal(7, tokens.Count); // tokens with whitespace + EOL + EOF
            
            AssertToken(tokens[0], TokenType.tkVar, "var", 1, 4, 6);
            AssertToken(tokens[1], TokenType.tkIdentifier, "x", 1, 10, 10);
            AssertToken(tokens[2], TokenType.tkColon, ":", 1, 14, 14);
            AssertToken(tokens[3], TokenType.tkIntegerKeyword, "integer", 1, 18, 24);
            AssertToken(tokens[4], TokenType.tkPrint, "print", 3, 5, 9);
            AssertToken(tokens[5], TokenType.tkIdentifier, "x", 3, 13, 13);
            AssertToken(tokens[6], TokenType.tkEOF, "", 4, 1, 1);
        }

        [Fact]
        public void TestMoreErrorCases()
        {
            var tokens = TokenizeFile("more_error_cases.txt");
            
            Assert.Equal(16, tokens.Count); // error tokens + EOL + EOF
            
            // 123.45.67.89
            AssertToken(tokens[0], TokenType.tkInvalid, "123.45.67.89", 1, 1, 12);
            AssertToken(tokens[1], TokenType.tkEOL, "\\n", 1, 13, 13);
            
            // 12.34.56
            AssertToken(tokens[2], TokenType.tkInvalid, "12.34.56", 2, 1, 8);
            AssertToken(tokens[3], TokenType.tkEOL, "\\n", 2, 9, 9);
            
            // abc.def.ghi.jkl
            AssertRecordAccessToken((RecordAccessToken)tokens[4], "abc.def.ghi.jkl", 3, 1, 15);
            AssertToken(tokens[5], TokenType.tkEOL, "\\n", 3, 16, 16);
            
            // @#$% - @#$ is one invalid token, % is mod operator
            AssertToken(tokens[6], TokenType.tkInvalid, "@#$", 4, 1, 3);
            AssertToken(tokens[7], TokenType.tkMod, "%", 4, 4, 4);
            AssertToken(tokens[8], TokenType.tkEOL, "\\n", 4, 5, 5);
            
            // 123abc - parsed as single invalid token
            AssertToken(tokens[9], TokenType.tkInvalid, "123abc", 5, 1, 6);
            AssertToken(tokens[10], TokenType.tkEOL, "\\n", 5, 7, 7);
            
            // abc123.456
            AssertRecordAccessToken((RecordAccessToken)tokens[11], "abc123.456", 6, 1, 10);
            AssertToken(tokens[12], TokenType.tkEOL, "\\n", 6, 11, 11);
            
            // abcf$k4j#@1 - identifier with invalid chars, all as one invalid token
            AssertToken(tokens[13], TokenType.tkInvalid, "abcf$k4j#@1", 7, 1, 11);
            AssertToken(tokens[14], TokenType.tkEOL, "\\n", 7, 12, 12);
            
            AssertToken(tokens[15], TokenType.tkEOF, "", 8, 1, 1);
        }

        [Fact]
        public void TestCompleteProgram()
        {
            var tokens = TokenizeFile("complete_program.txt");
            
            // This test verifies that a complete program can be tokenized correctly
            // We'll check for key tokens that should be present
            var tokenTypes = tokens.Select(t => t.Type).ToList();
            
            Assert.Contains(TokenType.tkType, tokenTypes);
            Assert.Contains(TokenType.tkRecord, tokenTypes);
            Assert.Contains(TokenType.tkRoutine, tokenTypes);
            Assert.Contains(TokenType.tkArray, tokenTypes);
            Assert.Contains(TokenType.tkFor, tokenTypes);
            Assert.Contains(TokenType.tkIf, tokenTypes);
            Assert.Contains(TokenType.tkPrint, tokenTypes);
            Assert.Contains(TokenType.tkRecordAccess, tokenTypes);
            Assert.Contains(TokenType.tkRange, tokenTypes);
            Assert.Contains(TokenType.tkEOF, tokenTypes);
        }
    }
}
