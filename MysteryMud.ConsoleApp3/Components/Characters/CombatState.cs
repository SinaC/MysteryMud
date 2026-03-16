using Arch.Core;

namespace MysteryMud.ConsoleApp3.Components.Characters;

struct CombatState
{
    public Entity Target;
    public int RoundDelay; // How many ticks until next action
}
