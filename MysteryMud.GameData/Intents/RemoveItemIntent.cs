using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct RemoveItemIntent
{
    public Entity Actor;
    public Entity Item;
    public EquipmentSlotKind Slot;
}