using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using TinyECS;

namespace MysteryMud.Domain.Action.Effect;

public class EffectLifecycleManager : IEffectLifecycleManager
{
    private readonly World _world;
    private readonly IDirtyTracker _dirtyTracker;

    public EffectLifecycleManager(World world, IDirtyTracker dirtyTracker)
    {
        _world = world;
        _dirtyTracker = dirtyTracker;
    }

    public void RemoveEffect(EntityId effect)
    {
        if (!_world.IsAlive(effect)) // don't use helpers, effect with ExpiredTag should be removable
            return;

        ref var instance = ref _world.Get<EffectInstance>(effect);
        var target = instance.Target;

        if (!_world.IsAlive(target))
            return;

        // Resolve which host to use — single branch point
        IEffectHost host = _world.Has<CharacterEffects>(target)
            ? new CharacterEffectHost(_world, _dirtyTracker, target)
            : new ItemEffectHost(_world, _dirtyTracker, target);

        host.UnregisterEffect(effect, instance.EffectRuntime);
    }
}
