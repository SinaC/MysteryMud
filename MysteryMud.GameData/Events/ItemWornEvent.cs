using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct ItemWornEvent
{
    public EntityId Entity;
    public EntityId Item;
    public EquipmentSlotKind Slot;
}
