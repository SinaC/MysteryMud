namespace MysteryMud.Core.Extensions;

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        if (input.Length == 1)
            return input.ToUpper();

        return char.ToUpper(input[0]) + input[1..];
    }

    public static int ComputeUniqueId(this string name)
        => name.AsSpan().ComputeUniqueId();

    public static string MaxLength(this string input, int length)
        => input?[..Math.Min(length, input.Length)] ?? string.Empty;
}
