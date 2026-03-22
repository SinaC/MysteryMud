namespace MysteryMud.ConsoleApp3.Commands.v2;

public class LiteralArgumentToken : ISyntaxToken
{
    private readonly string _word;
    private readonly string _name;
    public LiteralArgumentToken(string word, string name)
    {
        _word = word;
        _name = name;
    }

    public bool Match(ReadOnlySpan<char> input, Dictionary<string, object> args)
    {
        if (input.Equals(_word.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            args[_name] = _word;
            return true;
        }
        return false;
    }
}