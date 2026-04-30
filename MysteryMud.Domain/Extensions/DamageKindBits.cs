using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Extensions;

public static class DamageKindBits
{
    private static readonly DamageKind[] _allDamageKinds = Enum.GetValues<DamageKind>()
        .ToArray();

    public static ulong Set(this ulong bits, DamageKind kind)
    {
        return bits | (1UL << (int)kind);
    }

    public static ulong Clear(this ulong bits, DamageKind kind)
    {
        return bits & ~(1UL << (int)kind);
    }

    public static bool IsSet(this ulong bits, DamageKind kind)
    {
        return (bits & (1UL << (int)kind)) != 0;
    }

    public static ulong Toggle(this ulong bits, DamageKind kind)
    {
        return bits ^ (1UL << (int)kind);
    }

    public static string ToDamageKindString(this ulong bits, string separator = ", ")
    {
        var result = _allDamageKinds
            .Where(k => k != DamageKind.None && bits.IsSet(k))
            .Select(k => k.ToString());

        return string.Join(separator, result);
    }

    public static ulong ParseDamageKinds(string input, string separator = ",")
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        ulong bits = 0;

        var parts = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var token = part.Trim();

            if (Enum.TryParse<DamageKind>(token, ignoreCase: true, out var kind))
            {
                if (kind != DamageKind.None)
                {
                    bits |= (1UL << (int)kind);
                }
            }
            else
            {
                // You can choose behavior here:
                // throw, ignore, or log
                throw new ArgumentException($"Unknown DamageKind: '{token}'");
            }
        }

        return bits;
    }
}
