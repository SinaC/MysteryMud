using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;

namespace MysteryMud.Domain.Ability.Factories;

public static class ValidationRuleFactory
{
    public static IAbilityValidationRule Create(AbilityRuleDefinition def)
        => def switch
        {
            CharacterAffectedByRuleDefinition rule => new CharacterAffectedByRule(rule.EffectTagId, rule.FailBehaviour, rule.FailMessageKey),
            CharacterNotAffectedByRuleDefinition rule => new CharacterNotAffectedByRule(rule.EffectTagId, rule.FailBehaviour, rule.FailMessageKey),
            ItemAffectedByRuleDefinition rule => new ItemAffectedByRule(rule.EffectTagId, rule.FailBehaviour, rule.FailMessageKey),
            ItemNotAffectedByRuleDefinition rule => new ItemNotAffectedByRule(rule.EffectTagId, rule.FailBehaviour, rule.FailMessageKey),
            HasWeaponTypeRuleDefinition rule => new HasWeaponTypeRule(rule.Required, rule.FailBehaviour, rule.FailMessageKey),
            NotFightingRuleDefinition rule => new NotFightingRule(rule.FailBehaviour, rule.FailMessageKey),
            _ => throw new Exception($"Unknown validation rule type: {def.GetType()}")
        };
}
