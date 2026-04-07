using MysteryMud.Domain.Combat.Effect.Definitions;
using MysteryMud.Domain.Combat.Effect.Factories;

namespace MysteryMud.Domain.Combat.Effect;

public class EffectRegistry
{
    private readonly EffectRuntimeFactory _effectRuntimeFactory;

    private readonly Dictionary<int, EffectRuntime> EffectsById = [];
    private readonly Dictionary<string, EffectRuntime> EffectsByName = new(StringComparer.OrdinalIgnoreCase);

    public EffectRegistry(EffectRuntimeFactory effectRuntimeFactory)
    {
        _effectRuntimeFactory = effectRuntimeFactory;
    }

    public void RegisterEffects(IEnumerable<EffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            var effectRuntime = _effectRuntimeFactory.Create(effect);
            EffectsById.Add(effect.Id, effectRuntime);
            EffectsByName.Add(effect.Name, effectRuntime);
        }
    }

    public bool TryGetValue(int effectId, out EffectRuntime? effectRuntime)
        => EffectsById.TryGetValue(effectId, out effectRuntime);

    public bool TryGetValue(string effectName, out EffectRuntime? effectRuntime)
        => EffectsByName.TryGetValue(effectName, out effectRuntime);
}
