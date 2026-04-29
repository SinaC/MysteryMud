using DefaultEcs;

namespace MysteryMud.GameData.Events;

public struct LevelIncreasedEvent
{
    public Entity Target;
    public int Level;
}
