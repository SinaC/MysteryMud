using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Calculators;

public static class AggroCalculator
{
    public static int CalculateDamageAggro(Entity target, Entity source, int damageAmount, DamageKind damageKind)
    {
        return damageAmount; // TODO
    }

    public static int CalculateHealAggro(Entity target, Entity source, int healAmount)
    {
        return healAmount / 2; // TODO
    }
}
