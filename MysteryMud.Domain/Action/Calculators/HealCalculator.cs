using DefaultEcs;

namespace MysteryMud.Domain.Action.Calculators;

public class HealCalculator : IHealCalculator
{
    public decimal ModifyHeal(Entity target, decimal healAmount, Entity source)
    {
        return healAmount; // TODO: apply any healing modifiers here (buffs, debuffs, etc.)
    }
}
