namespace MysteryMud.ConsoleApp3.Commands.v2;

//public class GreedyArgumentToken : ISyntaxToken
//{
//    private readonly string Name;
//    public GreedyArgumentToken(string name) => Name = name;

//    public bool Match(string token, Dictionary<string, object> args)
//    {
//        // token here is the full remaining string
//        args[Name] = token;
//        return true;
//    }
//}

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