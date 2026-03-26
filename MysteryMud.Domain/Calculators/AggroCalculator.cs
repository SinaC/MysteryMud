using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Calculators;

public static class AggroCalculator
{
    public static int CalculateDamageAggro(Entity target, Entity source, int damageAmount, DamageTypes damageType)
    {
        return damageAmount; // TODO
    }

    public static int CalculateHealAggro(Entity target, Entity source, int healAmount)
    {
        return healAmount / 2; // TODO
    }
}
