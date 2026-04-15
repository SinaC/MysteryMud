using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;

namespace MysteryMud.Domain.Action.Effect;

public class EffectLifecycleManager : IEffectLifecycleManager
{
    public void RemoveEffect(GameState state, Entity effect)
    {
        if (!effect.IsAlive()) // don't use helpers, effect with ExpiredTag should be removable
            return;

        ref var instance = ref state.World.Get<EffectInstance>(effect);
        if (!instance.Target.IsAlive())
            return;

        var target = instance.Target;

        // Resolve which host to use — single branch point
        IEffectHost host = target.Has<CharacterEffects>()
            ? new CharacterEffectHost(target)
            : new ItemEffectHost(target);

        host.UnregisterEffect(state, effect, instance.EffectRuntime);
    }
}
