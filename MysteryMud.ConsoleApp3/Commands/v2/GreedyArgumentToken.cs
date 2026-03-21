namespace MysteryMud.ConsoleApp3.Commands.v2;

public class GreedyArgumentToken : ISyntaxToken
{
    private readonly string Name;
    public GreedyArgumentToken(string name) => Name = name;

    public bool Match(string token, Dictionary<string, object> args)
    {
        // token here is the full remaining string
        args[Name] = token;
        return true;
    }
}
