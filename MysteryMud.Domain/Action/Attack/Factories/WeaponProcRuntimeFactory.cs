using MysteryMud.Domain.Action.Attack.Definitions;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Action.Attack.Factories;

public static class WeaponProcRuntimeFactory
{
    public static WeaponProcRuntime Create(IEffectRegistry effectRegistry, WeaponProcDefinition definition)
    {
        var effectRuntimes = MapEffects(effectRegistry, definition);
        return new WeaponProcRuntime
        {
            Id = definition.Id,
            Name = definition.Name,
            Chance = definition.Chance,
            WeaponProcEffectRuntimes = effectRuntimes,
        };
    }

    private static List<WeaponProcEffectRuntime> MapEffects(IEffectRegistry effectRegistry, WeaponProcDefinition definition)
    {
        var effects = new List<WeaponProcEffectRuntime>();
        foreach (var effectDefinition in definition.EffectDefinitions)
        {
            if (!effectRegistry.TryGetRuntime(effectDefinition.EffectName, out var effectRuntime) || effectRuntime == null)
                throw new Exception($"Unknown effect {effectDefinition} found on weapon proc {definition.Name}");
            var effect = new WeaponProcEffectRuntime
            {
                EffectId = effectRuntime.Id,
                Target = effectDefinition.Target,
            };
            effects.Add(effect);
        }
        return effects;
    }
}
