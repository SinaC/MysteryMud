using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Calculators;

public static class AggroCalculator
{
    public static decimal CalculateDamageAggro(Entity target, Entity source, decimal damageAmount, DamageKind damageKind)
    {
        return damageAmount; // TODO
    }

    public static decimal CalculateHealAggro(Entity target, Entity source, decimal healAmount)
    {
        return healAmount / 2; // TODO
    }
}
