using Arch.Core;

namespace MysteryMud.Domain.Calculators;

public static class HealCalculator
{
    public static int ModifyDamage(Entity target, int healAmount, Entity source)
    {
        return healAmount; // TODO: apply any healing modifiers here (buffs, debuffs, etc.)
    }
}
