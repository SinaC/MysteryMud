using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect;

public class CharacterEffectHost : IEffectHost
{
    private readonly Entity _target;

    public CharacterEffectHost(Entity target)
    {
        _target = target;
    }

    public Entity Target => _target;

    public Entity? FindEffect(EffectRuntime effectRuntime)
    {
        ref var targetEffects = ref _target.Get<CharacterEffects>();

        if (effectRuntime.Tag.CharacterTag == CharacterEffectTagId.None)
        {
            foreach (var effect in targetEffects.Data.Effects)
            {
                ref var effectInstance = ref effect.Get<EffectInstance>();
                if (effectInstance.EffectRuntime.Name == effectRuntime.Name)
                    return effect;
            }
            return null;
        }
        var tagIndex = (int)effectRuntime.Tag.CharacterTag;
        ref var effectsByTag = ref targetEffects.Data.EffectsByTag[tagIndex];
        if (effectsByTag == null)
            return null;
        foreach (var effectByTag in effectsByTag)
        {
            ref var effectInstance = ref effectByTag.Get<EffectInstance>();
            if (effectInstance.EffectRuntime.Name == effectRuntime.Name)
                return effectByTag;
        }
        return null;
    }

    public void RegisterEffect(Entity effect, EffectRuntime effectRuntime)
    {
        // add effect to target effect's cache
        ref var targetEffects = ref _target.Get<CharacterEffects>();
        targetEffects.Data.Effects.Add(effect);

        // add tag if applicable
        if (effectRuntime.Tag.CharacterTag != CharacterEffectTagId.None)
        {
            var tagIndex = (int)effectRuntime.Tag.CharacterTag;
            // add EffectTag component to effect
            effect.Add(new CharacterEffectTag
            {
                Id = effectRuntime.Tag.CharacterTag
            });
            // add effect to target's CharacterEffects
            if (targetEffects.Data.EffectsByTag[tagIndex] == null)
                targetEffects.Data.EffectsByTag[tagIndex] = [effect];
            else
                targetEffects.Data.EffectsByTag[tagIndex]!.Add(effect);
            targetEffects.Data.ActiveTags |= 1UL << tagIndex;

            //_logger.LogInformation(LogEvents.Factory, " - add tag {tag}", effectRuntime.Tag);
        }
    }

    public void UnregisterEffect(GameState state, Entity effect, EffectRuntime effectRuntime)
    {
        if (!effect.IsAlive()) // don't use helpers, effect with ExpiredTag should be removable
            return;

        // remove the effect from the target's CharacterEffects
        ref var characterEffects = ref _target.Get<CharacterEffects>();
        characterEffects.Data.Effects.Remove(effect);

        // remove tag if applicable
        if (effectRuntime != null)
        {
            if (effectRuntime.Tag.CharacterTag != CharacterEffectTagId.None)
            {
                int tagIndex = (int)effectRuntime.Tag.CharacterTag;
                var effectsByTag = characterEffects.Data.EffectsByTag[tagIndex];
                if (effectsByTag != null)
                {
                    effectsByTag.Remove(effect);
                    if (effectsByTag.Count == 0)
                        characterEffects.Data.ActiveTags &= ~(1UL << tagIndex); // remove tag from active tags when last effect on that tag is removed
                }
            }
        }

        MarkAsDirtyIfNeeded(effect);

        // destroy effect
        state.World.Destroy(effect);
    }

    public void MarkAsDirtyIfNeeded(Entity effect)
    {
        // if effect has StatModifiers
        // flag the _target's stats as dirty so they will be recalculated without this effect
        if (effect.Has<CharacterStatModifiers>() && !_target.Has<DirtyStats>())
            _target.Add<DirtyStats>();

        // if effect has ResourceModifiers
        // flag the _target's resources as dirty so they will be recalculated without this effect
        if (effect.Has<CharacterResourceModifiers<HealthModifier>>() && !_target.Has<DirtyHealth>())
            _target.Add<DirtyHealth>();
        if (effect.Has<CharacterResourceModifiers<ManaModifier>>() && !_target.Has<DirtyMana>())
            _target.Add<DirtyMana>();
        if (effect.Has<CharacterResourceModifiers<EnergyModifier>>() && !_target.Has<DirtyEnergy>())
            _target.Add<DirtyEnergy>();
        if (effect.Has<CharacterResourceModifiers<RageModifier>>() && !_target.Has<DirtyRage>())
            _target.Add<DirtyRage>();

        // if effect has ResourceRegebModifiers
        // flag the _target's resource regens as dirty so they will be recalculated without this effect
        if (effect.Has<CharacterResourceRegenModifiers<HealthRegenModifier>>() && !_target.Has<DirtyHealthRegen>())
            _target.Add<DirtyHealthRegen>();
        if (effect.Has<CharacterResourceRegenModifiers<ManaRegenModifier>>() && !_target.Has<DirtyManaRegen>())
            _target.Add<DirtyManaRegen>();
        if (effect.Has<CharacterResourceRegenModifiers<EnergyModifier>>() && !_target.Has<DirtyEnergyRegen>())
            _target.Add<DirtyEnergyRegen>();
        if (effect.Has<CharacterResourceRegenModifiers<RageDecayModifier>>() && !_target.Has<DirtyRageDecay>())
            _target.Add<DirtyRageDecay>();
    }

    public EffectValuesSnapshot CreateSnapshot()
    {
        // snapshot target values
        var snapshottedValues = new EffectValuesSnapshot
        {
            TargetLevel = _target.Get<Level>().Value,
            TargetStats = _target.Get<EffectiveStats>().Values, // direct copy
        };
        return snapshottedValues;
    }

}
