namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

internal record AbilityValidationRulesData
(
    List<AbilityValidationRuleData> Source,
    List<AbilityValidationRuleData> Target
);
