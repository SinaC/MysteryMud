namespace MysteryMud.ConsoleApp3.Commands.v2;

//public class ArgumentToken : ISyntaxToken
//{
//    private readonly string Name;
//    private readonly IArgumentParser Parser;

//    public ArgumentToken(string name)
//    {
//        Name = name;
//        Parser = name switch
//        {
//            "item" => new ItemParser(),
//            "item1" => new ItemParser(),
//            "item2" => new ItemParser(),
//            "container" => new ContainerParser(),
//            "container1" => new ContainerParser(),
//            "container2" => new ContainerParser(),
//            "min" => new IntParser(),
//            "max" => new IntParser(),
//            "count" => new IntParser(),
//            "pet" => new ItemParser(), // could be same as item
//            "name" => new StringParser(),
//            "player" => new StringParser(),
//            "amount" => new IntParser(),
//            _ => throw new Exception($"Unknown argument type {name}")
//        };
//    }

//    public bool Match(string token, Dictionary<string, object> args)
//    {
//        if (Parser.TryParse(token, out var result))
//        {
//            args[Name] = result;
//            return true;
//        }
//        return false;
//    }
//}
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