using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;

namespace MysteryMud.Domain.Ability.Factories;

public static class ValidationRuleFactory
{
    public static IAbilityValidationRule Create(AbilityRuleDefinition def)
        => def switch
        {
            CharacterAffectedByRuleDefinition rule => new CharacterAffectedByRule(rule.Condition, rule.FailBehaviour, rule.FailMessageKey, rule.EffectTagId),
            CharacterNotAffectedByRuleDefinition rule => new CharacterNotAffectedByRule(rule.Condition, rule.FailBehaviour, rule.FailMessageKey, rule.EffectTagId),
            ItemAffectedByRuleDefinition rule => new ItemAffectedByRule(rule.Condition, rule.FailBehaviour, rule.FailMessageKey, rule.EffectTagId),
            ItemNotAffectedByRuleDefinition rule => new ItemNotAffectedByRule(rule.Condition, rule.FailBehaviour, rule.FailMessageKey, rule.EffectTagId),
            HasWeaponTypeRuleDefinition rule => new HasWeaponTypeRule(rule.Condition, rule.FailBehaviour, rule.FailMessageKey, rule.Required),
            NotFightingRuleDefinition rule => new NotFightingRule(rule.Condition, rule.FailBehaviour, rule.FailMessageKey),
            SavesSpellRuleDefinition rule => new SavesSpellRule(rule.Condition, rule.FailBehaviour, rule.FailMessageKey, rule.DamageKind),
            _ => throw new Exception($"Unknown validation rule type: {def.GetType()}")
        };
}
