using MysteryMud.Domain.Action.Effect.Definitions;

namespace MysteryMud.Domain.Action.Effect;

public interface IEffectRegistry
{
    void Register(IEnumerable<EffectDefinition> effects);

    bool TryGetDefinition(int effectId, out EffectDefinition? effectDefinition);
    bool TryGetRuntime(int effectId, out EffectRuntime? effectRuntime);
    bool TryGetRuntime(string effectName, out EffectRuntime? effectRuntime);
}
