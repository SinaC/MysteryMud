namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class OptionalToken : ISyntaxToken
{
    public readonly string _word;

    public OptionalToken(string word)
    {
        _word = word;
    }
}
