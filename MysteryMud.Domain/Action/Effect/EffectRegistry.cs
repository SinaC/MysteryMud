using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.Domain.Action.Effect.Factories;

namespace MysteryMud.Domain.Action.Effect;

public class EffectRegistry : IEffectRegistry
{
    private readonly EffectRuntimeFactory _effectRuntimeFactory;

    private readonly Dictionary<int, EffectDefinition> EffectDefinitionsById = [];
    private readonly Dictionary<int, EffectRuntime> EffectsById = [];
    private readonly Dictionary<string, EffectRuntime> EffectsByName = new(StringComparer.OrdinalIgnoreCase);

    public EffectRegistry(EffectRuntimeFactory effectRuntimeFactory)
    {
        _effectRuntimeFactory = effectRuntimeFactory;
    }

    public void Register(IEnumerable<EffectDefinition> effectDefinitions)
    {
        foreach (var effectDefinition in effectDefinitions)
        {
            var effectRuntime = _effectRuntimeFactory.Create(effectDefinition);
            EffectDefinitionsById.Add(effectDefinition.Id, effectDefinition);
            EffectsById.Add(effectDefinition.Id, effectRuntime);
            EffectsByName.Add(effectDefinition.Name, effectRuntime);
        }
    }

    public bool TryGetDefinition(int effectId, out EffectDefinition? effectDefinition)
       => EffectDefinitionsById.TryGetValue(effectId, out effectDefinition);

    public bool TryGetRuntime(int effectId, out EffectRuntime? effectRuntime)
        => EffectsById.TryGetValue(effectId, out effectRuntime);

    public bool TryGetRuntime(string effectName, out EffectRuntime? effectRuntime)
        => EffectsByName.TryGetValue(effectName, out effectRuntime);
}
