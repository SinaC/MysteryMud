using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct RemoveItemIntent
{
    public EntityId Entity;
    public EntityId Item;
    public EquipmentSlotKind Slot;
}