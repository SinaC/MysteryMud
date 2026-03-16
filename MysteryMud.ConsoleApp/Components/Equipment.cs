using Arch.Core;

namespace MysteryMud.ConsoleApp.Components;

struct Equipment
{
    public Dictionary<EquipSlot, Entity> Slots;
}

enum EquipSlot
{
    Weapon,
    Armor,
    Light
}
