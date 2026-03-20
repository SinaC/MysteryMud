using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DurationSystem
{
    public static void HandleExpiration(World world, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive())
        {
            // target is already dead, just clean up the effect
            world.Destroy(effect);
            return;
        }

        ref var duration = ref effect.Get<Duration>();

        if (duration.ExpirationTick != TimeSystem.CurrentTick)
        {
            Logger.Logger.Duration.Reschedule(effect, effectInstance.Target, duration.ExpirationTick);
            return;
        }

        Logger.Logger.Duration.Expire(effect, effectInstance.Target);

        // remove the effect from the target's CharacterEffects
        ref var characterEffects = ref effectInstance.Target.Get<CharacterEffects>();
        characterEffects.Effects.Remove(effect);
        if (effectInstance.Template.Tag != Data.Enums.EffectTagId.None)
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
            MessageSystem.Send(effectInstance.Target, effectInstance.Template.WearOffMessage);
        }

        world.Destroy(effect);
    }
}
