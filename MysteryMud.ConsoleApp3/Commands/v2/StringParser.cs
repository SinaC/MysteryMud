namespace MysteryMud.ConsoleApp3.Commands.v2;

public class StringParser : IArgumentParser
{
    public bool TryParse(string input, out object result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.Trim();

        // Remove surrounding quotes if present
        if ((input.StartsWith("'") && input.EndsWith("'")) ||
            (input.StartsWith("\"") && input.EndsWith("\"")))
        {
            input = input[1..^1];
        }

        result = input;
        return true;
    }
}
