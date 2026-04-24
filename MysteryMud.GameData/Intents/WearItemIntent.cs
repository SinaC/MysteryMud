using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct WearItemIntent
{
    public EntityId Entity;
    public EntityId Item;
    public EquipmentSlotKind Slot;
}
