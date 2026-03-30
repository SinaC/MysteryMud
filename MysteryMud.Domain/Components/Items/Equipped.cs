using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Items;

public struct Equipped
{
    public Entity Wearer;
    public EquipmentSlotKind Slot;
}
