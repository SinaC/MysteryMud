namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class GreedyArgumentToken : ISyntaxToken
{
    public string Name;

    public GreedyArgumentToken(string name)
    {
        Name = name;
    }
}