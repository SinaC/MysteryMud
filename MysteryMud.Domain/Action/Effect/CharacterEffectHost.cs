using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Action.Effect.Helpers;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Action.Effect;

public class CharacterEffectHost : IEffectHost
{
    private readonly World _world;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly EntityId _target;

    public CharacterEffectHost(World world, IDirtyTracker dirtyTracker, EntityId target)
    {
        _world = world;
        _dirtyTracker = dirtyTracker;
        _target = target;
    }

    public EntityId Target => _target;

    public EntityId? FindEffect(EffectRuntime effectRuntime)
    {
        ref var targetEffects = ref _world.Get<CharacterEffects>(_target);

        if (effectRuntime.Tag.CharacterTag == CharacterEffectTagId.None)
        {
            foreach (var effect in targetEffects.Data.Effects)
            {
                if (EffectHelpers.IsAlive(_world, effect)) continue;

                ref var effectInstance = ref _world.Get<EffectInstance>(effect);
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
            if (EffectHelpers.IsAlive(_world, effectByTag)) continue;

            ref var effectInstance = ref _world.Get<EffectInstance>(effectByTag);
            if (effectInstance.EffectRuntime.Name == effectRuntime.Name)
                return effectByTag;
        }
        return null;
    }

    public void RegisterEffect(EntityId effect, EffectRuntime effectRuntime)
    {
        // add effect to target effect's cache
        ref var targetEffects = ref _world.Get<CharacterEffects>(_target);
        targetEffects.Data.Effects.Add(effect);

        // add tag if applicable
        if (effectRuntime.Tag.CharacterTag != CharacterEffectTagId.None)
        {
            var tagIndex = (int)effectRuntime.Tag.CharacterTag;
            // add EffectTag component to effect
            _world.Add(effect, new CharacterEffectTag
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

        //
        if (_world.Has<PlayerTag>(_target))
            _dirtyTracker.MarkDirty(_target, DirtyReason.Effects);
    }

    public void UnregisterEffect(EntityId effect, EffectRuntime effectRuntime)
    {
        if (!_world.IsAlive(effect)) // don't use helpers, effect with ExpiredTag should be removable
            return;

        // remove the effect from the target's CharacterEffects
        ref var characterEffects = ref _world.Get<CharacterEffects>(_target);
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

        // destroy effect: cannot delete an entity from where -> soft delete
        if (!_world.Has<ExpiredTag>(effect))
            _world.Add<ExpiredTag>(effect);
    }

    public void MarkAsDirtyIfNeeded(EntityId effect)
    {
        // if effect has StatModifiers
        // flag the _target's stats as dirty so they will be recalculated without this effect
        if (_world.Has<CharacterStatModifiers>(effect) && !_world.Has<DirtyStats>(_target))
            _world.Add<DirtyStats>(_target);

        // if effect has ResourceModifiers
        // flag the _target's resources as dirty so they will be recalculated without this effect
        if (_world.Has<CharacterResourceModifiers<HealthModifier>>(effect) && !_world.Has<DirtyHealth>(_target))
            _world.Add<DirtyHealth>(_target);
        if (_world.Has<CharacterResourceModifiers<ManaModifier>>(effect) && !_world.Has<DirtyMana>(_target))
            _world.Add<DirtyMana>(_target);
        if (_world.Has<CharacterResourceModifiers<EnergyModifier>>(effect) && !_world.Has<DirtyEnergy>(_target))
            _world.Add<DirtyEnergy>(_target);
        if (_world.Has<CharacterResourceModifiers<RageModifier>>(effect) && !_world.Has<DirtyRage>(_target))
            _world.Add<DirtyRage>(_target);

        // if effect has ResourceRegebModifiers
        // flag the _target's resource regens as dirty so they will be recalculated without this effect
        if (_world.Has<CharacterResourceRegenModifiers<HealthRegenModifier>>(effect) && !_world.Has<DirtyHealthRegen>(_target))
            _world.Add<DirtyHealthRegen>(_target);
        if (_world.Has<CharacterResourceRegenModifiers<ManaRegenModifier>>(effect) && !_world.Has<DirtyManaRegen>(_target))
            _world.Add<DirtyManaRegen>(_target);
        if (_world.Has<CharacterResourceRegenModifiers<EnergyModifier>>(effect) && !_world.Has<DirtyEnergyRegen>(_target))
            _world.Add<DirtyEnergyRegen>(_target);
        if (_world.Has<CharacterResourceRegenModifiers<RageDecayModifier>>(effect) && !_world.Has<DirtyRageDecay>(_target))
            _world.Add<DirtyRageDecay>(_target);

        //
        if (_world.Has<PlayerTag>(_target))
            _dirtyTracker.MarkDirty(_target, DirtyReason.Effects);
    }

    public EffectValuesSnapshot CreateSnapshot()
    {
        // snapshot target values
        var snapshottedValues = new EffectValuesSnapshot
        {
            TargetLevel = _world.Get<Level>(_target).Value,
            TargetStats = _world.Get<EffectiveStats>(_target).Values, // direct copy
        };
        return snapshottedValues;
    }

}
