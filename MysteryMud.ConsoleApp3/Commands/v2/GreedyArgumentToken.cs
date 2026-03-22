namespace MysteryMud.ConsoleApp3.Commands.v2;

public class GreedyArgumentToken : ISyntaxToken
{
    private readonly string _name;
    public GreedyArgumentToken(string name) => _name = name;

    public bool Match(ReadOnlySpan<char> input, Dictionary<string, object> args)
    {
        args[_name] = input.ToString(); // capture remaining input as string
        return true;
    }
}