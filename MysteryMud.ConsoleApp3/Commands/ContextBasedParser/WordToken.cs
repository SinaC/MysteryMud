namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class WordToken : ISyntaxToken
{
    public readonly string _word; // TODO: change

    public WordToken(string word)
    {
        _word = word;
    }
}