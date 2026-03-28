using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct ItemGivenEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity Target;
}
