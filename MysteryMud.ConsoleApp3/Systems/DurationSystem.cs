using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DurationSystem
{
    public static void Update(World world)
    {
        var query = new QueryDescription()
                .WithAll<EffectInstance, Duration>();
        world.Query(query, (Entity effect,
            ref EffectInstance effectInstance, ref Duration duration) =>
        {
            Console.WriteLine($"Processing Duration for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName} with remaining ticks {duration.RemainingTicks}");

            duration.RemainingTicks--;
            if (duration.RemainingTicks > 0)
                return;

            Console.WriteLine($"Wearing off Duration for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName}");

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
        });
    }
}
