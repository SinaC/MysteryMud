using DefaultEcs;

namespace MysteryMud.Domain.Action.Calculators;

public static class HealCalculator
{
    public static decimal ModifyHeal(Entity target, decimal healAmount, Entity source)
    {
        return healAmount; // TODO: apply any healing modifiers here (buffs, debuffs, etc.)
    }
}
