using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Components.Characters;

public struct Equipment
{
    public Dictionary<EquipmentSlotKind, EntityId> Slots;
}
