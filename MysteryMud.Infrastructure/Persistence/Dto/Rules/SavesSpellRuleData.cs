namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

internal class SavesSpellRuleData : AbilityValidationRuleData
{
    public required string DamageKind { get; init; } = default!;
}
