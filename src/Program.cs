using Compiler.Lexer;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            const string input = @"
                var person : record
                    salary : real
                    age : integer
                end
                
                person.name := 2351.11
                person.age := 30
                
                for i in person.age..50 loop
                    print i
                end
            ";

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