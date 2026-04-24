using TinyECS;

namespace MysteryMud.Domain.Components.Characters;

public struct CombatState
{
    public EntityId Target;
    public int RoundDelay; // How many ticks until next action
}
