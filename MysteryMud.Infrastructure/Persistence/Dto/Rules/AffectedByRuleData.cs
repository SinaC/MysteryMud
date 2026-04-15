namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

internal class AffectedByRuleData : AbilityValidationRuleData
{
    public string TagKind { get; init; } = default!; // Character, Item
    public required string Tag { get; init; }
}
