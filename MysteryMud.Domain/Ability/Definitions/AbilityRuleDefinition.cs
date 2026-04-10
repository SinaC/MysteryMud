using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public abstract class AbilityRuleDefinition
{
    public required AbilityValidationRuleScope Scope { get; init; }
    public required AbilityValidationRuleFailActions FailActions { get; init; }
    public required string FailMessageKey { get; init; }
}
