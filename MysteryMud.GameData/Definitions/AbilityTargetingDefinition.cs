using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public sealed class AbilityTargetingDefinition
{
    public AbilityTargetRequirement Requirement { get; init; } = AbilityTargetRequirement.Mandatory;
    public AbilityTargetSelection Selection { get; init; } = AbilityTargetSelection.Single;

    // Ordered list of search contexts. For Single selection the resolver
    // tries each context in order, returning the first match.
    // For AoE all contexts are swept and results are unioned.
    public List<AbilityTargetingContextDefinition> Contexts { get; init; } = [new ()]; // default: one context room/character

    // Defaults: Single → CastStart (enforced), AoE → CastCompletion.
    // Loader should normalise: if Selection==Single, always set CastStart.
    public AbilityTargetResolveAt ResolveAt { get; init; } = AbilityTargetResolveAt.CastStart;
}
