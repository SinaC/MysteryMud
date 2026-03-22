namespace MysteryMud.ConsoleApp3.Commands.v2;

public class ArgumentToken : ISyntaxToken
{
    private readonly string _name;
    public ArgumentToken(string name) => _name = name;

    public bool Match(ReadOnlySpan<char> input, Dictionary<string, object> args)
    {
        // Numeric parsing or string storage
        if (int.TryParse(input, out int intVal))
            args[_name] = intVal;
        else
            args[_name] = input.ToString(); // optional allocation only here
        return true;
    }
}