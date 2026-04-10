using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Ability.Factories;

public static class AbilityRuntimeFactory
{
    public static AbilityRuntime Create(EffectRegistry effectRegistry, AbilityExecutionResolverRegistry abilityExecutionResolverRegistry, AbilityDefinition def)
    {
        if (def.Effects == null || def.Effects.Count == 0)
            throw new Exception($"No effect found on ability {def.Name}");
        if (!abilityExecutionResolverRegistry.TryGetResolver(def.Executor ?? "Default", out var registeredResolver) || registeredResolver == null)
            throw new Exception($"Unknown executor {def.Executor} on ability {def.Name}");
        var effectIds = MapEffectIds(effectRegistry, def, def.Effects);
        var failureEffectIds = MapEffectIds(effectRegistry, def, def.FailureEffects);
        var rules = MapValidationRules(def);
        return new AbilityRuntime
        {
            Id = def.Id,
            Name = def.Name,
            Kind = def.Kind,
            CastTime = def.CastTime,
            Cooldown = def.Cooldown,
            Costs = def.Costs,
            Targeting = def.Targeting,
            ExecutorId = registeredResolver.Id,
            EffectIds = effectIds,
            FailureEffectIds = failureEffectIds,
            Messages = def.Messages,
            ValidationRules = rules,
        };
    }

    private static List<IAbilityValidationRule> MapValidationRules(AbilityDefinition def)
    {
        var rules = new List<IAbilityValidationRule>();
        foreach (var ruleDef in def.ValidationRules)
        {
            var rule = ValidationRuleFactory.Create(ruleDef);
            rules.Add(rule);
        }
        return rules;
    }

    private static List<int> MapEffectIds(EffectRegistry effectRegistry, AbilityDefinition def, IEnumerable<string> effectNames)
    {
        var effectIds = new List<int>();
        foreach (var effectName in effectNames)
        {
            if (!effectRegistry.TryGetValue(effectName, out var effectRuntime) || effectRuntime == null)
                throw new Exception($"Unknown effect {effectName} found on ability {def.Name}");
            effectIds.Add(effectRuntime.Id);
        }
        return effectIds;
    }
}
