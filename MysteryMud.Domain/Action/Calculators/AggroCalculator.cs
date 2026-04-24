using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Calculators;

public static class AggroCalculator
{
    public static int CalculateDamageAggro(EntityId target, EntityId source, int damageAmount, DamageKind damageKind)
    {
        return damageAmount; // TODO
    }

    public static int CalculateHealAggro(EntityId target, EntityId source, int healAmount)
    {
        return healAmount / 2; // TODO
    }
}
