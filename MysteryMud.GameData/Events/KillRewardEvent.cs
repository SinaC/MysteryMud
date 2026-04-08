using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct KillRewardEvent
{
    public Entity Killer;
    public Entity Victim;
    public bool GrantXp;
    public bool GrantLoop;
}
