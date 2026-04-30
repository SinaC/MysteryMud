using DefaultEcs;

namespace MysteryMud.Domain.Action.Calculators;

public interface IHealCalculator
{
    decimal ModifyHeal(Entity target, decimal healAmount, Entity source);
}