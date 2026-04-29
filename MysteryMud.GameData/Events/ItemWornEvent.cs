using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct ItemWornEvent
{
    public Entity Entity;
    public Entity Item;
    public EquipmentSlotKind Slot;
}
