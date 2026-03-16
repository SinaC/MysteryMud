using Arch.Core;

namespace MysteryMud.ConsoleApp2.ECS.Components.Characters;

public struct Equipment
{
    public Dictionary<EquipSlot, Entity> Slots;
}
