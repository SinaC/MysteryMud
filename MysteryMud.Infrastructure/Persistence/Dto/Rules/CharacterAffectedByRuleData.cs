namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

internal class CharacterAffectedByRuleData : AbilityValidationRuleData
{
    public required string Tag { get; init; }
}
