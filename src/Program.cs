using Compiler.Lexer;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            const string input = @"type Address is record
                    var streetCode : integer
                    var number : integer
                end

                type Person is record
                    var age : integer
                    var addr : Address
                end

                type IntArray is array[5] integer
                var arr : IntArray is [1, 2, 3, 4, 5]
                
                person.name:=2351.11
                person.age:=30
                
                for i in person.age..50 loop
                    print i
                end
                if ( person.age = 5 ) then
                    print 5
                end";

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