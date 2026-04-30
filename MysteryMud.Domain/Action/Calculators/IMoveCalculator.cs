using DefaultEcs;

namespace MysteryMud.Domain.Action.Calculators
{
    public interface IMoveCalculator
    {
        decimal ModifyRestoreMove(Entity target, decimal moveAmount, Entity source);
    }
}