using Compiler.Lexer;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string input = File.ReadAllText("/Users/user/Documents/BSUIR/Innopolis/programming/ProjectI/compiler/src/test.imperative");

            var lexer = new LexerClass(input);
            var tokens = lexer.NextToken();

            while (tokens.Type != TokenType.tkEOF)
            {
                Console.WriteLine(tokens);
                tokens = lexer.NextToken();
            }
        }
    }
}