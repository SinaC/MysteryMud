using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Extensions;

public static class DamageKindBits
{
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
}
