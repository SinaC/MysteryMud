using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct RemoveItemIntent
{
    public Entity Entity;
    public Entity Item;
    public EquipmentSlotKind Slot;
}