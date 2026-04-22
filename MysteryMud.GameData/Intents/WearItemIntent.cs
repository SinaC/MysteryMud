using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct WearItemIntent
{
    public Entity Entity;
    public Entity Item;
    public EquipmentSlotKind Slot;
}
