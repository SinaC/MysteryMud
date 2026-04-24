using TinyECS;

namespace MysteryMud.Domain.Action.Calculators;

public static class MoveCalculator
{
    public static decimal ModifyRestoreMove(EntityId target, EntityId source, decimal moveAmount)
    {
        return moveAmount; // TODO: apply any restore move modifiers here (buffs, debuffs, etc.)
    }
}
