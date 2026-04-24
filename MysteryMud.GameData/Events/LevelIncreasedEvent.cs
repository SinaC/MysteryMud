using TinyECS;

namespace MysteryMud.GameData.Events;

public struct LevelIncreasedEvent
{
    public EntityId Target;
    public int Level;
}
