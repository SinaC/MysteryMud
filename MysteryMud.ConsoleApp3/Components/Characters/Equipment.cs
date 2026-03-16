using Arch.Core;
using MysteryMud.ConsoleApp3.Enums;

namespace MysteryMud.ConsoleApp3.Components.Characters;

struct Equipment
{
    public Dictionary<EquipmentSlot, Entity> Slots;
}
