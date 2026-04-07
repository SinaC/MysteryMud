using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Ability.Factories;

public static class AbilityRuntimeFactory
{
    public static AbilityRuntime Create(EffectRegistry effectRegistry, AbilityDefinition def)
    {
        if (def.Effects == null || def.Effects.Count == 0)
            throw new Exception($"No effect found on ability {def.Name}");
        var effectIds = new List<int>();
        foreach (var effectName in def.Effects)
        {
            if (!effectRegistry.TryGetValue(effectName, out var effectRuntime) || effectRuntime == null)
                throw new Exception($"Unknown effect {effectName} found on ability {def.Name}");
            effectIds.Add(effectRuntime.Id);
        }
        return new AbilityRuntime
        {
            Id = def.Id,
            Name = def.Name,
            Kind = def.Kind,
            CastTime = def.CastTime,
            Cooldown = def.Cooldown,
            Costs = def.Costs,
            EffectIds = effectIds,
        };
    }
}
