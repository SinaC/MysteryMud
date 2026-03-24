using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain;
using MysteryMud.Domain.Data.Enums;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;

namespace MysteryMud.Application.Systems;

public static class DurationSystem
{
    public static void HandleExpiration(SystemContext ctx, GameState state, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive())
        {
            // target is already dead, just clean up the effect
            state.World.Destroy(effect);
            return;
        }

        ref var duration = ref effect.Get<Duration>();

        if (duration.ExpirationTick != TimeSystem.CurrentTick)
        {
            ctx.Log.LogInformation(LogEvents.Duration,"Rescheduled Duration for Effect {effectName} on Target {targetName} with Expiration Tick {expirationTick}", effect.DebugName, effectInstance.Target.DebugName, duration.ExpirationTick);
            return;
        }

        ctx.Log.LogInformation(LogEvents.Duration,"Expiring Duration for Effect {effectName} on Target {targetName}", effect.DebugName, effectInstance.Target.DebugName);

        // remove the effect from the target's CharacterEffects
        ref var characterEffects = ref effectInstance.Target.Get<CharacterEffects>();
        characterEffects.Effects.Remove(effect);
        if (effectInstance.Template.Tag != EffectTagId.None)
        {
            int tagIndex = (int)effectInstance.Template.Tag;
            if (characterEffects.EffectsByTag[tagIndex] == effect)
            {
                characterEffects.EffectsByTag[tagIndex] = null;
                characterEffects.ActiveTags &= ~(1UL << tagIndex);
            }
        }

        // flag the target's stats as dirty so they will be recalculated without this effect
        ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
        if (hasStatModifiers && !effectInstance.Target.Has<DirtyStats>())
            effectInstance.Target.Add<DirtyStats>();

        if (effectInstance.Template.WearOffMessage != null)
        {
            // TODO: in room ?
            ctx.MessageBus.Publish(effectInstance.Target, effectInstance.Template.WearOffMessage);
        }

        state.World.Destroy(effect);
    }
}
