using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability;

public class AbilityRuntime
{
    public required int Id { get; init; } // generated
    public required string Name { get; init; }
    public required AbilityKind Kind { get; init; }
    public required int CastTime { get; init; } // 0 means instant cast
    public required int Cooldown { get; init; }
    public required List<ResourceCost> Costs { get; init; }
    public required int ExecutorId { get; init; }
    public required List<int> EffectIds { get; init; }
    public required List<int> FailureEffectIds { get; init; } = [];
    public required Dictionary<string, string> Messages { get; init; } = []; // TODO: actor/room messages ?
    public required List<IAbilityValidationRule> ValidationRules { get; init; } = [];
}
