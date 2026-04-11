namespace MysteryMud.Infrastructure.Persistence;

public static class EnumParser
{
    public static T Parse<T>(string value, T defaultValue)
        where T : struct, Enum
    {
        if (value == null)
            return defaultValue;
        return Enum.Parse<T>(value, ignoreCase: true);
    }
}
