using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public struct AbilityValidationResult
{
    public bool Success;
    public AbilityValidationRuleFailActions FailActions;
    public string? FailureMessageKey; // key like "OnBlockedByCalm"
}