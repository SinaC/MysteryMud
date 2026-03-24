using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Components.Items;

struct Equipped
{
    public Entity Wearer;
    public EquipmentSlot Slot;
}
