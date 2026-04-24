using TinyECS;

namespace MysteryMud.Domain.Action.Calculators;

public static class HealCalculator
{
    public static decimal ModifyHeal(EntityId target, EntityId source, decimal healAmount)
    {
        return healAmount; // TODO: apply any healing modifiers here (buffs, debuffs, etc.)
    }
}
