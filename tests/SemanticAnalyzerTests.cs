using Xunit;
using Compiler.Lexer;
using Compiler.Parser;
using Compiler.AST;
using Compiler.Semantic;

namespace Compiler.Tests
{
    public class SemanticAnalyzerTests
    {
        private SemanticAnalyzer AnalyzeCode(string code)
        {
            var lexer = new LexerClass(code);
            var parser = new ParserFacade(lexer);
            var ast = parser.Parse();
            
            var analyzer = new SemanticAnalyzer();
            analyzer.Analyze(ast);
            
            return analyzer;
        }

        private SemanticAnalyzer AnalyzeFile(string fileName)
        {
            string filePath = Path.Combine("semantic_test_files", fileName);
            string code = File.ReadAllText(filePath);
            return AnalyzeCode(code);
        }

        [Fact]
        public void TestForwardDeclaration_ValidSignature()
        {
            var analyzer = AnalyzeFile("forward_declaration_valid.txt");
            
            Assert.False(analyzer.HasErrors);
            Assert.Empty(analyzer.Errors);
        }

        [Fact]
        public void TestForwardDeclaration_InvalidParameterType()
        {
            var analyzer = AnalyzeFile("forward_declaration_invalid_signature.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("Signature", errorMessages);
        }

        [Fact]
        public void TestForwardDeclaration_InvalidReturnType()
        {
            var analyzer = AnalyzeFile("forward_declaration_invalid_return_type.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("Signature", errorMessages);
        }

        [Fact]
        public void TestForwardDeclaration_InvalidParameterCount()
        {
            var analyzer = AnalyzeFile("forward_declaration_invalid_param_count.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("Signature", errorMessages);
        }

        [Fact]
        public void TestReturn_CompatibleType()
        {
            var analyzer = AnalyzeFile("return_compatible_type.txt");
            
            Assert.False(analyzer.HasErrors);
            Assert.Empty(analyzer.Errors);
        }

        [Fact]
        public void TestReturn_IncompatibleType()
        {
            var analyzer = AnalyzeFile("return_incompatible_type.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("Return type mismatch", errorMessages);
        }

        [Fact]
        public void TestReturn_NonexistentVariable()
        {
            var analyzer = AnalyzeFile("return_nonexistent_variable.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("Undeclared variable", errorMessages);
        }

        [Fact]
        public void TestReturn_ConditionalAllPaths()
        {
            var analyzer = AnalyzeFile("return_conditional_all_paths.txt");
            
            Assert.False(analyzer.HasErrors);
            Assert.Empty(analyzer.Errors);
        }

        [Fact]
        public void TestReturn_ConditionalMissingPath()
        {
            var analyzer = AnalyzeFile("return_conditional_missing_path.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("Not all execution paths", errorMessages);
        }

        [Fact]
        public void TestReturn_VoidWithValue()
        {
            var analyzer = AnalyzeFile("return_void_with_value.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("does not return a value", errorMessages);
        }

        [Fact]
        public void TestReturn_WithoutValue()
        {
            var analyzer = AnalyzeFile("return_without_value.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("expects a return value", errorMessages);
        }

        [Fact]
        public void TestReturn_IntegerLiteral()
        {
            var analyzer = AnalyzeFile("return_integer_literal.txt");
            
            Assert.False(analyzer.HasErrors);
            Assert.Empty(analyzer.Errors);
        }

        [Fact]
        public void TestReturn_RealToInteger()
        {
            var analyzer = AnalyzeFile("return_real_to_integer.txt");
            
            Assert.True(analyzer.HasErrors);
            Assert.NotEmpty(analyzer.Errors);
            
            var errorMessages = string.Join("; ", analyzer.Errors.Select(e => e.Message));
            Assert.Contains("Return type mismatch", errorMessages);
        }
    }
}

