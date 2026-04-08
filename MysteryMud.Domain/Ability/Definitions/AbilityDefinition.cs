using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class AbilityDefinition
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required AbilityKind Kind { get; init; } // spell/skill/passive/weapon
    public required int CastTime { get; init; } // 0 means instant cast
    public required int Cooldown { get; init; }
    public required List<ResourceCost> Costs { get; init; }
    public required List<string> Effects { get; init; }

    public CommandDefinition? Command { get; init; }
}
