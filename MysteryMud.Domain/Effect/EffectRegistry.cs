namespace MysteryMud.Domain.Effect;

public class EffectRegistry
{
    private readonly Dictionary<int, EffectRuntime> EffectsById = [];
    private readonly Dictionary<string, EffectRuntime> EffectsByName = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterEffects(IEnumerable<EffectRuntime> effects)
    {
        foreach (var effect in effects)
        {
            EffectsById.Add(effect.Id, effect);
            EffectsByName.Add(effect.Name, effect);
        }
    }

    public bool TryGetValue(int effectId, out EffectRuntime? effectRuntime)
        => EffectsById.TryGetValue(effectId, out effectRuntime);

    public bool TryGetValue(string effectName, out EffectRuntime? effectRuntime)
        => EffectsByName.TryGetValue(effectName, out effectRuntime);
}
