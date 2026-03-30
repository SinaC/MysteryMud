using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct ItemWornEvent
{
    public Entity Actor;
    public Entity Item;
    public EquipmentSlotKind Slot;
}
