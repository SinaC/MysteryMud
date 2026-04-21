using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct ItemRemovedEvent
{
    public Entity Entity;
    public Entity Item;
    public EquipmentSlotKind Slot;
}