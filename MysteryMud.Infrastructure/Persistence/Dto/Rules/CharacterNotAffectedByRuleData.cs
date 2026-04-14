namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

internal class CharacterNotAffectedByRuleData : AbilityValidationRuleData
{
    public required string Tag { get; init; }
}
