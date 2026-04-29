using DefaultEcs;

namespace MysteryMud.GameData.Events;

public struct ItemGivenEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity Target;
}
