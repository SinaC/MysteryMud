using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct DeathEvent
{
    public Entity Dead;
    public Entity Killer;
}
