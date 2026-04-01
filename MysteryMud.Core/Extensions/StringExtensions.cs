namespace MysteryMud.Core.Extensions;

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        if (input.Length == 1)
            return input.ToUpper();

        return char.ToUpper(input[0]) + input.Substring(1);
    }

    public static int ComputeCommandId(this string name)
        => name.AsSpan().ComputeCommandId();
}
