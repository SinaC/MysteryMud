namespace MysteryMud.ConsoleApp3.Commands.v2;

public class IntParser : IArgumentParser
{
    public bool TryParse(string input, out object result)
    {
        result = null;
        return int.TryParse(input, out int val) && (result = val) != null;
    }
}
