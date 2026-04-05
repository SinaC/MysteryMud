using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class AbilityDefinition
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required AbilityKind Kind { get; init; } // spell/skill/passive/weapon
    public required int CastTime { get; init; } // 0 means instant cast
    public required int Cooldown { get; init; }
    public required int ResourceCost { get; init; } // only mana for the moment
    public required List<string> Effects { get; init; }
}
