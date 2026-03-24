using Arch.Core;

namespace MysteryMud.Application.Simulation.Calculators;

public static class HealCalculator
{
    public static int ModifyDamage(Entity target, int healAmount, Entity source)
    {
        return healAmount; // TODO: apply any healing modifiers here (buffs, debuffs, etc.)
    }
}
