using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Ability.Factories;

public class AbilityRuntimeFactory : IAbilityRuntimeFactory
{
    private readonly IValidationRuleFactory _validationRuleFactory;

    public AbilityRuntimeFactory(IValidationRuleFactory validationRuleFactory)
    {
        _validationRuleFactory = validationRuleFactory;
    }

    public AbilityRuntime Create(IEffectRegistry effectRegistry, IAbilityOutcomeResolverRegistry abilityOutcomeResolverRegistry, AbilityDefinition def)
    {
        if (def.ConditionalEffectGroups == null || def.ConditionalEffectGroups.Count == 0)
            throw new Exception($"No effect found on ability {def.Name}");

        var outcomeResolver = MapAbilityOutcomeResolver(abilityOutcomeResolverRegistry, def);
        var conditionalEffectGroupIds = MapConditionEffectGroupIds(effectRegistry, def);
        var failureEffectIds = MapEffectIds(effectRegistry, def, def.FailureEffects);
        var sourceValidationRules = MapValidationRules(def.SourceValidationRules);
        var targetValidationRules = MapValidationRules(def.TargetValidationRules);
        return new AbilityRuntime
        {
            Id = def.Id,
            Name = def.Name,
            Kind = def.Kind,
            CastTime = def.CastTime,
            Cooldown = def.Cooldown,
            Costs = def.Costs,
            Targeting = def.Targeting,
            OutcomeResolver = outcomeResolver,
            ConditionalEffectGroups = conditionalEffectGroupIds,
            FailureEffectIds = failureEffectIds,
            Messages = def.Messages,
            SourceValidationRules = sourceValidationRules,
            TargetValidationRules = targetValidationRules,
        };
    }

    private static AbilityOutcomeResolverRuntime? MapAbilityOutcomeResolver(IAbilityOutcomeResolverRegistry abilityExecutionResolverRegistry, AbilityDefinition def)
    {
        if (def.OutcomeResolver == null)
            return null;
        if (!abilityExecutionResolverRegistry.TryGetResolver(def.OutcomeResolver.ResolverName ?? "Default", out var registeredResolver) || registeredResolver == null)
            throw new Exception($"Unknown outcome resolver {def.OutcomeResolver} on ability {def.Name}");
        return new AbilityOutcomeResolverRuntime
        {
            ResolverId = registeredResolver.Id,
            Hook = def.OutcomeResolver.Hook,
        };
    }

    private List<IAbilityValidationRule> MapValidationRules(IEnumerable<AbilityRuleDefinition> ruleDefinitions)
    {
        var rules = new List<IAbilityValidationRule>();
        foreach (var ruleDef in ruleDefinitions)
        {
            var rule = _validationRuleFactory.Create(ruleDef);
            rules.Add(rule);
        }
        return rules;
    }

    private static List<AbilityConditionalEffectGroupRuntime> MapConditionEffectGroupIds(IEffectRegistry effectRegistry, AbilityDefinition def)
    {
        var conditionalEffectGroups = new List<AbilityConditionalEffectGroupRuntime>();
        foreach (var conditionalEffectGroupDefinition in def.ConditionalEffectGroups)
        {
            var effectIds = MapEffectIds(effectRegistry, def, conditionalEffectGroupDefinition.Effects);
            var conditionalEffectGroup = new AbilityConditionalEffectGroupRuntime
            {
                Condition = conditionalEffectGroupDefinition.Condition,
                EffectIds = effectIds
            };
            conditionalEffectGroups.Add(conditionalEffectGroup);
        }
        return conditionalEffectGroups;
    }

    private static List<int> MapEffectIds(IEffectRegistry effectRegistry, AbilityDefinition def, IEnumerable<string> effectNames)
    {
        var effectIds = new List<int>();
        foreach (var effectName in effectNames)
        {
            if (!effectRegistry.TryGetRuntime(effectName, out var effectRuntime) || effectRuntime == null)
                throw new Exception($"Unknown effect {effectName} found on ability {def.Name}");
            effectIds.Add(effectRuntime.Id);
        }
        return effectIds;
    }
}
