using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability;

public class AbilityRuntime
{
    public required int Id { get; init; } // generated
    public required string Name { get; init; }
    public required AbilityKind Kind { get; init; }
    public required int CastTime { get; init; } // 0 means instant cast
    public required int Cooldown { get; init; }
    public required int ResourceCost { get; init; }
    public required List<int> EffectIds { get; init; }
}
