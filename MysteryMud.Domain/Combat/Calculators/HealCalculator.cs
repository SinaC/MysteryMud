using Arch.Core;

namespace MysteryMud.Domain.Combat.Calculators;

public static class HealCalculator
{
    public static decimal ModifyHeal(Entity target, decimal healAmount, Entity source)
    {
        return healAmount; // TODO: apply any healing modifiers here (buffs, debuffs, etc.)
    }
}
