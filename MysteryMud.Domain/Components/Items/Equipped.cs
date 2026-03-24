using Arch.Core;
using MysteryMud.Domain.Data.Enums;

namespace MysteryMud.Domain.Components.Items;

public struct Equipped
{
    public Entity Wearer;
    public EquipmentSlot Slot;
}
