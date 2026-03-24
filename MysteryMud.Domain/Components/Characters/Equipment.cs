using Arch.Core;
using MysteryMud.Domain.Data.Enums;

namespace MysteryMud.Domain.Components.Characters;

public struct Equipment
{
    public Dictionary<EquipmentSlot, Entity> Slots;
}
