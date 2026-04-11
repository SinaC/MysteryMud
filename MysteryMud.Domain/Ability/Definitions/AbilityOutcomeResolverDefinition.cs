using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class AbilityOutcomeResolverDefinition
{
    public required string ResolverName { get; init; } = "default";
    public required AbilityOutcomeHook Hook { get; init; }
}
