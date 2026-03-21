namespace MysteryMud.ConsoleApp3.Commands.v2;

public class WordToken : ISyntaxToken
{
    private readonly string _word;

    public WordToken(string word)
    {
        _word = word;
    }

    public bool Match(ReadOnlySpan<char> input, Dictionary<string, object> args)
    {
        return input.Equals(_word.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }
}
