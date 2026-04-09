using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;

namespace MysteryMud.Domain.Ability.Factories;

public static class ValidationRuleFactory
{
    public static IAbilityValidationRule Create(AbilityRuleDefinition def)
        => def switch
        {
            AffectedByRuleDefinition rule => new AffectedByRule(rule.EffectTagId, rule.FailMessageKey),
            HasWeaponTypeRuleDefinition rule => new HasWeaponTypeRule(rule.Required, rule.FailMessageKey),
            NotAffectedByRuleDefinition rule => new NotAffectedByRule(rule.EffectTagId, rule.FailMessageKey),
            TargetNotFightingRuleDefinition rule => new TargetNotFightingRule(rule.FailMessageKey),
            _ => throw new Exception($"Unknown validation rule type: {def.GetType()}")
        };
}
