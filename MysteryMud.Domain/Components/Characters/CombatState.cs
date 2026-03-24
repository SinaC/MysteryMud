using Arch.Core;

namespace MysteryMud.Domain.Components.Characters;

public struct CombatState
{
    public Entity Target;
    public int RoundDelay; // How many ticks until next action
}
