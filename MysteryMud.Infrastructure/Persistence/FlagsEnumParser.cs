namespace MysteryMud.Infrastructure.Persistence;

public static class FlagsEnumParser
{
    public static TEnum Parse<TEnum>(
     string? value,
     TEnum defaultValue,
     IReadOnlyDictionary<string, TEnum>? aliases = null)
     where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        var span = value.AsSpan();
        TEnum result = default;
        bool any = false;

        foreach (var range in span.Split(','))
        {
            var trimmed = span[range].Trim();
            if (trimmed.IsEmpty) continue;

            if (aliases is not null)
            {
                var tokenStr = trimmed.ToString();
                if (aliases.TryGetValue(tokenStr, out var aliasValue))
                {
                    result = Or(result, aliasValue);
                    any = true;
                    continue;
                }
            }

            if (Enum.TryParse<TEnum>(trimmed.ToString(), ignoreCase: true, out var parsed))
            {
                result = Or(result, parsed);
                any = true;
            }
            else
            {
                throw new FormatException(
                    $"'{trimmed.ToString()}' is not a valid value for {typeof(TEnum).Name}.");
            }
        }

        return any ? result : defaultValue;
    }

    // Enum doesn't expose | operator generically pre-.NET 7
    private static TEnum Or<TEnum>(TEnum a, TEnum b) where TEnum : struct, Enum
        => (TEnum)Enum.ToObject(typeof(TEnum),
               Convert.ToInt64(a) | Convert.ToInt64(b));
}