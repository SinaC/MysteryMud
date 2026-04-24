namespace MysteryMud.Core.Extensions;

public static class Pluralizer
{
    private static readonly Dictionary<string, string> Irregulars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "man", "men" },
        { "woman", "women" },
        { "child", "children" },
        { "foot", "feet" },
        { "tooth", "teeth" },
        { "mouse", "mice" },
        { "goose", "geese" },
        { "person", "people" }
    };

    public static string Pluralize(string noun)
    {
        if (string.IsNullOrWhiteSpace(noun))
            return noun;

        // Check irregulars first
        if (Irregulars.TryGetValue(noun, out var irregular))
            return irregular;

        // Words ending in 'y' → 'ies' (but not vowel + y)
        if (noun.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
            noun.Length > 1 &&
            !"aeiou".Contains(char.ToLower(noun[noun.Length - 2])))
        {
            return noun.Substring(0, noun.Length - 1) + "ies";
        }

        // Words ending in s, x, z, ch, sh → add "es"
        if (noun.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            noun.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            noun.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
            noun.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            noun.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return noun + "es";
        }

        // Words ending in 'f' or 'fe' → 'ves' (common cases)
        if (noun.EndsWith("fe", StringComparison.OrdinalIgnoreCase))
        {
            return noun.Substring(0, noun.Length - 2) + "ves";
        }
        if (noun.EndsWith("f", StringComparison.OrdinalIgnoreCase))
        {
            return noun.Substring(0, noun.Length - 1) + "ves";
        }

        // Default: just add 's'
        return noun + "s";
    }
}