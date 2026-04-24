using MysteryMud.Domain.Components.Effects;
using TinyECS;

namespace MysteryMud.Domain.Action.Effect;

public interface IEffectHost
{
    EntityId Target { get; }

    EntityId? FindEffect(EffectRuntime effectRuntime);

    void RegisterEffect(EntityId effect, EffectRuntime effectRuntime);
    void UnregisterEffect(EntityId effect, EffectRuntime effectRuntime);

    // Dirty if needed
    void MarkAsDirtyIfNeeded(EntityId effect);

    // Snapshot for formula evaluation
    EffectValuesSnapshot CreateSnapshot();
}
