using Arch.Core;

namespace MysteryMud.Domain.Action.Calculators;

public static class MoveCalculator
{
    public static decimal ModifyRestoreMove(Entity target, decimal moveAmount, Entity source)
    {
        return moveAmount; // TODO: apply any restore move modifiers here (buffs, debuffs, etc.)
    }
}
