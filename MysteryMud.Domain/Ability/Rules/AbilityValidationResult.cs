namespace MysteryMud.Domain.Ability.Rules;

public struct AbilityValidationResult
{
    public bool Success;
    public string? FailureMessageKey; // key like "OnBlockedByCalm"
}