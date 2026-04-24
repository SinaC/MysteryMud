using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Items;

public struct Equipped
{
    public EntityId Wearer;
    public EquipmentSlotKind Slot;
}
