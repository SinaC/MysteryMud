using DefaultEcs;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;

namespace MysteryMud.Domain.Action.Effect;

public class EffectLifecycleManager : IEffectLifecycleManager
{
    private readonly IDirtyTracker _dirtyTracker;

    public EffectLifecycleManager(IDirtyTracker dirtyTracker)
    {
        _dirtyTracker = dirtyTracker;
    }

    public void RemoveEffect(Entity effect)
    {
        if (!effect.IsAlive) // don't use helpers, effect with ExpiredTag should be removable
            return;

        ref var instance = ref effect.Get<EffectInstance>();
        if (!instance.Target.IsAlive)
            return;

        var target = instance.Target;

        // Resolve which host to use — single branch point
        IEffectHost host = target.Has<CharacterEffects>()
            ? new CharacterEffectHost(_dirtyTracker, target)
            : new ItemEffectHost(_dirtyTracker, target);

        host.UnregisterEffect(effect, instance.EffectRuntime);
    }
}
