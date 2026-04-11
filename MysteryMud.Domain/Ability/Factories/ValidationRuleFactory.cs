using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;

namespace MysteryMud.Domain.Ability.Factories;

public static class ValidationRuleFactory
{
    public static IAbilityValidationRule Create(AbilityRuleDefinition def)
        => def switch
        {
            AffectedByRuleDefinition rule => new AffectedByRule(rule.EffectTagId, rule.FailBehaviour, rule.FailMessageKey),
            HasWeaponTypeRuleDefinition rule => new HasWeaponTypeRule(rule.Required, rule.FailBehaviour, rule.FailMessageKey),
            NotAffectedByRuleDefinition rule => new NotAffectedByRule(rule.EffectTagId, rule.FailBehaviour, rule.FailMessageKey),
            NotFightingRuleDefinition rule => new NotFightingRule(rule.FailBehaviour, rule.FailMessageKey),
            _ => throw new Exception($"Unknown validation rule type: {def.GetType()}")
        };
}
