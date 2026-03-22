namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public static class ItemArgParser
{
    public static bool TryParse(ReadOnlySpan<char> input, out ItemArg result)
    {
        result = default;

        // all
        if (input.Equals("all".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            result = new ItemArg { All = true };
            return true;
        }

        int dotIndex = input.IndexOf('.');
        // sword
        if (dotIndex < 0)
        {
            result = new ItemArg { Name = input.ToString() };
            return true;
        }

        var left = input[..dotIndex];
        var right = input[(dotIndex + 1)..];

        // all.sword
        if (left.Equals("all".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            result = new ItemArg { Name = right.ToString(), AllOf = true };
            return true;
        }

        // 5.sword
        if (int.TryParse(left, out int index))
        {
            result = new ItemArg { Name = right.ToString(), Index = index };
            return true;
        }

        return false;
    }
}
