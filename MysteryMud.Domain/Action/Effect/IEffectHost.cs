using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Effects;

namespace MysteryMud.Domain.Action.Effect;

public interface IEffectHost
{
    Entity Target { get; }

    Entity? FindEffect(EffectRuntime effectRuntime);

    void RegisterEffect(Entity effect, EffectRuntime effectRuntime);
    void UnregisterEffect(GameState state, Entity effect, EffectRuntime effectRuntime);

    // Dirty if needed
    void MarkAsDirtyIfNeeded(Entity effect);

    // Snapshot for formula evaluation
    EffectValuesSnapshot CreateSnapshot();
}
