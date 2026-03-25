using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Characters;

public struct Equipment
{
    public Dictionary<EquipmentSlot, Entity> Slots;
}
