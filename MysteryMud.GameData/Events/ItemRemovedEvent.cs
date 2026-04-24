using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct ItemRemovedEvent
{
    public EntityId Entity;
    public EntityId Item;
    public EquipmentSlotKind Slot;
}