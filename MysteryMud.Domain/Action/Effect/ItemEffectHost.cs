using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Action.Effect.Helpers;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Action.Effect;

public class ItemEffectHost : IEffectHost
{
    private readonly World _world;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly EntityId _target;

    public ItemEffectHost(World world, IDirtyTracker dirtyTracker, EntityId target)
    {
        _world = world;
        _dirtyTracker = dirtyTracker;
        _target = target;
    }

    public EntityId Target => _target;

    public EntityId? FindEffect(EffectRuntime effectRuntime)
    {
        ref var targetEffects = ref _world.Get<ItemEffects>(_target);

        if (effectRuntime.Tag.ItemTag == ItemEffectTagId.None)
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
        var tagIndex = (int)effectRuntime.Tag.ItemTag;
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
        ref var targetEffects = ref _world.Get<ItemEffects>(_target);
        targetEffects.Data.Effects.Add(effect);

        // add tag if applicable
        if (effectRuntime.Tag.ItemTag != ItemEffectTagId.None)
        {
            var tagIndex = (int)effectRuntime.Tag.ItemTag;
            // add EffectTag component to effect
            _world.Add(effect, new ItemEffectTag
            {
                Id = effectRuntime.Tag.ItemTag
            });
            // add effect to target's ItemEffects
            if (targetEffects.Data.EffectsByTag[tagIndex] == null)
                targetEffects.Data.EffectsByTag[tagIndex] = [effect];
            else
                targetEffects.Data.EffectsByTag[tagIndex]!.Add(effect);
            targetEffects.Data.ActiveTags |= 1UL << tagIndex;

            //_logger.LogInformation(LogEvents.Factory, " - add tag {tag}", effectRuntime.Tag);
        }

        // if item is worn, check character stat/resource/resource regen modifiers
        ref var equipped = ref _world.TryGetRef<Equipped>(_target, out var isEquipped);
        if (isEquipped)
        {
            var wearer = equipped.Wearer;
            if (CharacterHelpers.IsAlive(_world, wearer) && _world.Has<PlayerTag>(wearer))
            {
                _dirtyTracker.MarkDirty(wearer, DirtyReason.Effects);
            }
        }
    }

    public void UnregisterEffect(EntityId effect, EffectRuntime effectRuntime)
    {
        if (!_world.IsAlive(effect)) // don't use helpers, effect with ExpiredTag should be removable
            return;

        // remove the effect from the target's ItemEffects
        ref var itemEffects = ref _world.Get<ItemEffects>(_target);
        itemEffects.Data.Effects.Remove(effect);

        // remove tag if applicable
        if (effectRuntime != null)
        {
            if (effectRuntime.Tag.ItemTag != ItemEffectTagId.None)
            {
                int tagIndex = (int)effectRuntime.Tag.ItemTag;
                var effectsByTag = itemEffects.Data.EffectsByTag[tagIndex];
                if (effectsByTag != null)
                {
                    effectsByTag.Remove(effect);
                    if (effectsByTag.Count == 0)
                        itemEffects.Data.ActiveTags &= ~(1UL << tagIndex); // remove tag from active tags when last effect on that tag is removed
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
        // TODO:
        // ItemStatModifiers: for item specific modifiers
        // ItemResourceModifiers: for item with a dedicated resource
        // ItemResourceRegenModifiers: for item with a dedicated resource

        // if item is worn, check character stat/resource/resource regen modifiers
        ref var equipped = ref _world.TryGetRef<Equipped>(_target, out var isEquipped);
        if (isEquipped)
        {
            var wearer = equipped.Wearer;
            if (CharacterHelpers.IsAlive(_world, wearer))
            {
                // if effect has StatModifiers
                // flag the wearer's stats as dirty so they will be recalculated without this effect
                if (_world.Has<CharacterStatModifiers>(effect) && !_world.Has<DirtyStats>(wearer))
                    _world.Add<DirtyStats>(wearer);

                // if effect has ResourceModifiers
                // flag the wearer's resources as dirty so they will be recalculated without this effect
                if (_world.Has<CharacterResourceModifiers<HealthModifier>>(effect) && !_world.Has<DirtyHealth>(wearer))
                    _world.Add<DirtyHealth>(wearer);
                if (_world.Has<CharacterResourceModifiers<ManaModifier>>(effect) && !_world.Has<DirtyMana>(wearer))
                    _world.Add<DirtyMana>(wearer);
                if (_world.Has<CharacterResourceModifiers<EnergyModifier>>(effect) && !_world.Has<DirtyEnergy>(wearer))
                    _world.Add<DirtyEnergy>(wearer);
                if (_world.Has<CharacterResourceModifiers<RageModifier>>(effect) && !_world.Has<DirtyRage>(wearer))
                    _world.Add<DirtyRage>(wearer);

                // if effect has ResourceRegebModifiers
                // flag the wearer's resource regens as dirty so they will be recalculated without this effect
                if (_world.Has<CharacterResourceRegenModifiers<HealthRegenModifier>>(effect) && !_world.Has<DirtyHealthRegen>(wearer))
                    _world.Add<DirtyHealthRegen>(wearer);
                if (_world.Has<CharacterResourceRegenModifiers<ManaRegenModifier>>(effect) && !_world.Has<DirtyManaRegen>(wearer))
                    _world.Add<DirtyManaRegen>(wearer);
                if (_world.Has<CharacterResourceRegenModifiers<EnergyModifier>>(effect) && !_world.Has<DirtyEnergyRegen>(wearer))
                    _world.Add<DirtyEnergyRegen>(wearer);
                if (_world.Has<CharacterResourceRegenModifiers<RageDecayModifier>>(effect) && !_world.Has<DirtyRageDecay>(wearer))
                    _world.Add<DirtyRageDecay>(wearer);
            }

            if (_world.Has<PlayerTag>(wearer))
                _dirtyTracker.MarkDirty(wearer, DirtyReason.Effects);
        }
    }

    public EffectValuesSnapshot CreateSnapshot()
    {
        // snapshot target values
        return new EffectValuesSnapshot
        {
            ItemLevel = _world.Get<Level>(_target).Value,
            // TODO: other values depending on item type
        };
    }
}
