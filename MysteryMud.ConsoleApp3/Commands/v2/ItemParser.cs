namespace MysteryMud.ConsoleApp3.Commands.v2;

public class ItemParser : IArgumentParser
{
    public bool TryParse(string input, out object result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(input)) return false;
        input = input.Trim();

        if (input.StartsWith("all."))
        {
            result = new ItemArg { All = true, Name = input.Substring(4) };
            return true;
        }

        var parts = input.Split('.', 2);
        if (parts.Length == 2 && int.TryParse(parts[0], out int index))
        {
            result = new ItemArg { Index = index, Name = parts[1] };
            return true;
        }

        result = new ItemArg { Name = input };
        return true;
    }
}
