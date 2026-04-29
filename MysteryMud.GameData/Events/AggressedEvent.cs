using DefaultEcs;

namespace MysteryMud.GameData.Events;

public struct AggressedEvent
{
    public Entity Source;
    public Entity Target;
}
