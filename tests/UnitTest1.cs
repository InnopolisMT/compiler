using Compiler.Lexer;
using Newtonsoft.Json;
using Xunit;

namespace MyProjectTests;

public class UnitTest1
{
    private static readonly string input = File.ReadAllText("/Users/timofeykurstak/compiler/src/test.imperative");
    private static readonly LexerClass lexer = new LexerClass(input);
    private static int currentIndex = 0;

    [Theory]
    [MemberData(nameof(TokenTestCases))]
    public void Test2(Token expected)
    {
        var actual = lexer.NextToken();
        currentIndex++;
        Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actual));
    }

    public static IEnumerable<object[]> TokenTestCases()
    {
        yield return new object[] { new IntegerToken("4", 4, new Span(1, 1, 1)) };
        yield return new object[] { new IdentifierToken("f", new Span(1, 2, 2)) };
    }
}