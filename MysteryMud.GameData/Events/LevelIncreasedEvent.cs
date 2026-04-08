using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct LevelIncreasedEvent
{
    public Entity Target;
    public int Level;
}
