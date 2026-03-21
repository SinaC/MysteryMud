namespace MysteryMud.ConsoleApp3.Commands.v2;

public class WordToken : ISyntaxToken
{
    private readonly string Word;
    public WordToken(string word) => Word = word;
    public bool Match(string token, Dictionary<string, object> args)
        => token.Equals(Word, StringComparison.OrdinalIgnoreCase);
}
