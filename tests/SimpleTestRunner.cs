using System;
using System.IO;
using Compiler.Lexer;

namespace Compiler.Tests
{
    public class SimpleTestRunner
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("Running Lexer Tests");
            Console.WriteLine("==========================================");

            var testRunner = new SimpleTestRunner();
            int passed = 0;
            int failed = 0;

            // Run basic tests
            passed += testRunner.RunTest("Basic Identifiers", testRunner.TestBasicIdentifiers);
            passed += testRunner.RunTest("Integer Literals", testRunner.TestIntegerLiterals);
            passed += testRunner.RunTest("Real Literals", testRunner.TestRealLiterals);
            passed += testRunner.RunTest("Boolean Literals", testRunner.TestBooleanLiterals);
            passed += testRunner.RunTest("Keywords", testRunner.TestKeywords);
            passed += testRunner.RunTest("Operators", testRunner.TestOperators);
            passed += testRunner.RunTest("Record Access", testRunner.TestRecordAccess);
            passed += testRunner.RunTest("Type Declarations", testRunner.TestTypeDeclarations);
            passed += testRunner.RunTest("Array Declarations", testRunner.TestArrayDeclarations);
            passed += testRunner.RunTest("Control Structures", testRunner.TestControlStructures);
            passed += testRunner.RunTest("Complex Cases", testRunner.TestComplexCases);
            passed += testRunner.RunTest("Error Cases", testRunner.TestErrorCases);
            passed += testRunner.RunTest("Complete Program", testRunner.TestCompleteProgram);

            failed = 13 - passed;

            Console.WriteLine("==========================================");
            Console.WriteLine($"Test Summary: {passed} passed, {failed} failed");
            Console.WriteLine("==========================================");
        }

        private int RunTest(string testName, Func<bool> testFunc)
        {
            try
            {
                Console.Write($"Running {testName}... ");
                bool result = testFunc();
                if (result)
                {
                    Console.WriteLine("PASSED");
                    return 1;
                }
                else
                {
                    Console.WriteLine("FAILED");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - Exception: {ex.Message}");
                return 0;
            }
        }

        private List<Token> TokenizeFile(string fileName)
        {
            // Get the directory where the test project is located
            string? testDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (testDir == null)
                throw new InvalidOperationException("Could not determine test directory");
            
            string filePath = Path.Combine(testDir, "test_files", fileName);
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

        public bool TestBasicIdentifiers()
        {
            var tokens = TokenizeFile("basic_identifiers.txt");
            return tokens.Count == 3 && 
                   tokens[0].Type == TokenType.tkIdentifier && 
                   tokens[0].Lexeme == "variable";
        }

        public bool TestIntegerLiterals()
        {
            var tokens = TokenizeFile("integer_literals.txt");
            return tokens.Count == 5 && 
                   tokens[0].Type == TokenType.tkIntegerLiteral && 
                   tokens[0].Lexeme == "123";
        }

        public bool TestRealLiterals()
        {
            var tokens = TokenizeFile("real_literals.txt");
            return tokens.Count == 5 && 
                   tokens[0].Type == TokenType.tkRealLiteral && 
                   tokens[0].Lexeme == "123.45";
        }

        public bool TestBooleanLiterals()
        {
            var tokens = TokenizeFile("boolean_literals.txt");
            return tokens.Count == 4 && 
                   tokens[0].Type == TokenType.tkBoolLiteral && 
                   tokens[0].Lexeme == "true";
        }

        public bool TestKeywords()
        {
            var tokens = TokenizeFile("keywords.txt");
            return tokens.Count == 12 && 
                   tokens[0].Type == TokenType.tkVar && 
                   tokens[0].Lexeme == "var";
        }

        public bool TestOperators()
        {
            var tokens = TokenizeFile("operators.txt");
            return tokens.Count == 15 && 
                   tokens[0].Type == TokenType.tkPlus && 
                   tokens[0].Lexeme == "+";
        }

        public bool TestRecordAccess()
        {
            var tokens = TokenizeFile("record_access.txt");
            return tokens.Count == 5 && 
                   tokens[0].Type == TokenType.tkRecordAccess && 
                   tokens[0].Lexeme == "person.name";
        }

        public bool TestTypeDeclarations()
        {
            var tokens = TokenizeFile("type_declarations.txt");
            return tokens.Count == 13 && 
                   tokens[0].Type == TokenType.tkType && 
                   tokens[0].Lexeme == "type";
        }

        public bool TestArrayDeclarations()
        {
            var tokens = TokenizeFile("array_declarations.txt");
            return tokens.Count == 10 && 
                   tokens[0].Type == TokenType.tkType && 
                   tokens[0].Lexeme == "type";
        }

        public bool TestControlStructures()
        {
            var tokens = TokenizeFile("control_structures.txt");
            return tokens.Count == 25 && 
                   tokens[0].Type == TokenType.tkIf && 
                   tokens[0].Lexeme == "if";
        }

        public bool TestComplexCases()
        {
            var tokens = TokenizeFile("complex_cases.txt");
            return tokens.Count == 14 && 
                   tokens[0].Type == TokenType.tkFor && 
                   tokens[0].Lexeme == "for";
        }

        public bool TestErrorCases()
        {
            var tokens = TokenizeFile("error_cases.txt");
            return tokens.Count == 5 && 
                   tokens[0].Type == TokenType.tkInvalid && 
                   tokens[0].Lexeme == "123.45.67";
        }

        public bool TestCompleteProgram()
        {
            var tokens = TokenizeFile("complete_program.txt");
            var tokenTypes = tokens.Select(t => t.Type).ToList();
            
            return tokenTypes.Contains(TokenType.tkType) &&
                   tokenTypes.Contains(TokenType.tkRecord) &&
                   tokenTypes.Contains(TokenType.tkRoutine) &&
                   tokenTypes.Contains(TokenType.tkArray) &&
                   tokenTypes.Contains(TokenType.tkFor) &&
                   tokenTypes.Contains(TokenType.tkIf) &&
                   tokenTypes.Contains(TokenType.tkPrint) &&
                   tokenTypes.Contains(TokenType.tkRecordAccess) &&
                   tokenTypes.Contains(TokenType.tkRange) &&
                   tokenTypes.Contains(TokenType.tkEOF);
        }
    }
}
