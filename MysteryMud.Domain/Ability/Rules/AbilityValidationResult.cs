using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public struct AbilityValidationResult
{
    public bool Success;

    public AbilityValidationFailBehaviour FailBehaviour;
    public string? FailMessageKey;
}