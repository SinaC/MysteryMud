using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public abstract class AbilityRuleDefinition
{
    public TargetCondition Condition { get; init; } = TargetCondition.None;

    // Only meaningful for target rules; source rules always abort.
    public AbilityValidationFailBehaviour FailBehaviour { get; init; } = AbilityValidationFailBehaviour.Abort;

    // Key into the ability's Messages dictionary.
    // For source rules: always sent on failure.
    // For target rules: sent when OnFail == SkipWithMessage.
    public required string FailMessageKey { get; init; }
}
