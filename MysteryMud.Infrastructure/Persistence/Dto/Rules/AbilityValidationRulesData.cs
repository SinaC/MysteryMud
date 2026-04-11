namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

public record AbilityValidationRulesData
(
    List<AbilityValidationRuleData> Source,
    List<AbilityValidationRuleData> Target
);
