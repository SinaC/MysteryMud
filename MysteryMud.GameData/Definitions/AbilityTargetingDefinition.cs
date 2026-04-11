namespace MysteryMud.GameData.Enums;

public sealed class AbilityTargetingDefinition
{
    public AbilityTargetRequirement Requirement { get; init; } = AbilityTargetRequirement.Mandatory;
    public AbilityTargetSelection Selection { get; init; } = AbilityTargetSelection.Single;
    public AbilityTargetScope Scope { get; init; } = AbilityTargetScope.Room;
    public AbilityTargetFilter Filter { get; init; } = AbilityTargetFilter.Character;

    // Defaults: Single → CastStart (enforced), AoE → CastCompletion.
    // Loader should normalise: if Selection==Single, always set CastStart.
    public AbilityTargetResolveAt ResolveAt { get; init; } = AbilityTargetResolveAt.CastStart;
}
