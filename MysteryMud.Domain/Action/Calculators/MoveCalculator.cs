using DefaultEcs;

namespace MysteryMud.Domain.Action.Calculators;

public class MoveCalculator : IMoveCalculator
{
    public decimal ModifyRestoreMove(Entity target, decimal moveAmount, Entity source)
    {
        return moveAmount; // TODO: apply any restore move modifiers here (buffs, debuffs, etc.)
    }
}
