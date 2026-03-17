using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Data;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Factories;

public static class EffectFactory
{
    public static void ApplyEffect(World world, EffectTemplate effectTemplate, Entity source, Entity target)
    {
        ref var targetEffects = ref target.Get<CharacterEffects>();

        // if effect has a tag, check for an existing effect with the same tag and apply stacking rules if found
        int tagIndex = (int)effectTemplate.Tag;
        if (effectTemplate.Tag != EffectTagId.None && targetEffects.EffectsByTag[tagIndex] is Entity existing)
        {
            var handled = HandleStacking(world, effectTemplate, existing, source);
            if (handled)
                return;
            // not handled: apply new effect
        }

        // no tag or no existing effect with the same tag, create a new one
        var effect = world.Create(new EffectInstance
        {
            Source = source,
            Target = target,
            StackCount = 1,
            Template = effectTemplate,
        });

        // add effect to target effect cache
        targetEffects.Effects.Add(effect);

        // add tag if applicable
        if (effectTemplate.Tag != EffectTagId.None)
        {
            effect.Add(new EffectTag
            {
                Id = effectTemplate.Tag
            });
            targetEffects.EffectsByTag[tagIndex] = effect;
            targetEffects.ActiveTags |= 1UL << tagIndex;
        }

        // duration ?
        var duration = effectTemplate.DurationFunc?.Invoke(world, source, target);
        if (duration is not null)
            effect.Add(new Duration { RemainingTicks = duration.Value });

        // stat modifiers ?
        if (effectTemplate.StatModifiers is not null && effectTemplate.StatModifiers.Length > 0)
        {
            var modifiers = new List<StatModifier>();
            foreach (var modifierDefinition in effectTemplate.StatModifiers)
            {
                var modifier = new StatModifier
                {
                    Stat = modifierDefinition.Stat,
                    Type = modifierDefinition.Type,
                    Value = modifierDefinition.Value,
                };
                modifiers.Add(modifier);
            }
            effect.Add(new StatModifiers
            {
                Values = modifiers
            });
            // add dirty flag to character stats so we will recalculate them with the new modifiers
            if (!target.Has<DirtyStats>())
                target.Add<DirtyStats>();
        }

        // TODO: remove
        ref var casterStats = ref source.Get<EffectiveStats>();

        // dot ?
        if (effectTemplate.Dot is not null)
            effect.Add(new DamageOverTime
            {
                Damage = effectTemplate.Dot.Value.DamageFunc.Invoke(world, source, target),
                DamageType = effectTemplate.Dot.Value.DamageType,
                TickRate = effectTemplate.Dot.Value.TickRate,
                NextTick = TimeSystem.CurrentTick + effectTemplate.Dot.Value.TickRate
            });

        // hot ?
        if (effectTemplate.Hot is not null)
            effect.Add(new HealOverTime
            {
                Heal = effectTemplate.Hot.Value.HealFunc.Invoke(world, source, target),
                TickRate = effectTemplate.Hot.Value.TickRate,
                NextTick = TimeSystem.CurrentTick + effectTemplate.Hot.Value.TickRate
            });

        // apply message
        if (effectTemplate.ApplyMessage != null)
            MessageSystem.Send(source, effectTemplate.ApplyMessage);
    }

    private static bool HandleStacking(World world, EffectTemplate effectTemplate, Entity effect, Entity source)
    {
        ref var effectInstance = ref world.Get<EffectInstance>(effect);

        if (source != effectInstance.Source)
        {
            // if the source is different, we will treat it as a new effect and not apply stacking rules
            return false; // not handled, allow new effect to be applied
        }

        ref var duration = ref effect.TryGetRef<Duration>(out var hasDuration);
        int idx = (int)effectTemplate.Tag;

        switch (effectTemplate.Stacking)
        {
            case StackingRule.None:
                // if the stacking rule is None, do not apply the new effect and do not refresh the duration
                return true; // handled
            case StackingRule.Refresh:
                if (hasDuration)
                    duration.RemainingTicks = effectTemplate.DurationFunc?.Invoke(world, effectInstance.Source, effectInstance.Target) ?? 0;
                return true; // handled
            case StackingRule.Stack:
                if (effectInstance.StackCount < effectTemplate.MaxStacks)
                    effectInstance.StackCount++;
                if (hasDuration)
                    duration.RemainingTicks = effectTemplate.DurationFunc?.Invoke(world, effectInstance.Source, effectInstance.Target) ?? 0;
                return true; // handled
        }

        return true; // default to handled to prevent new effect application if stacking rule is not recognized
    }
}
