using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;

namespace MysteryMud.Domain.Ability.Factories;

public static class ValidationRuleFactory
{
    public static IAbilityValidationRule Create(AbilityRuleDefinition def)
        => def switch
        {
            AffectedByRuleDefinition rule => new AffectedByRule(rule.EffectTagId, rule.FailActions, rule.FailMessageKey),
            HasWeaponTypeRuleDefinition rule => new HasWeaponTypeRule(rule.Required, rule.FailActions, rule.FailMessageKey),
            NotAffectedByRuleDefinition rule => new NotAffectedByRule(rule.EffectTagId, rule.FailActions, rule.FailMessageKey),
            NotFightingRuleDefinition rule => new NotFightingRule(rule.FailActions, rule.FailMessageKey),
            _ => throw new Exception($"Unknown validation rule type: {def.GetType()}")
        };
}
