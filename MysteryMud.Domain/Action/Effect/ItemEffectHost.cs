using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect;

public class ItemEffectHost : IEffectHost
{
    private readonly Entity _target;

    public ItemEffectHost(Entity target)
    {
        _target = target;
    }

    public Entity Target => _target;

    public Entity? FindEffect(EffectRuntime effectRuntime)
    {
        ref var targetEffects = ref _target.Get<ItemEffects>();

        if (effectRuntime.Tag.ItemTag == ItemEffectTagId.None)
        {
            foreach (var effect in targetEffects.Data.Effects)
            {
                ref var effectInstance = ref effect.Get<EffectInstance>();
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
            ref var effectInstance = ref effectByTag.Get<EffectInstance>();
            if (effectInstance.EffectRuntime.Name == effectRuntime.Name)
                return effectByTag;
        }
        return null;
    }

    public void RegisterEffect(Entity effect, EffectRuntime effectRuntime)
    {
        // add effect to target effect's cache
        ref var targetEffects = ref _target.Get<ItemEffects>();
        targetEffects.Data.Effects.Add(effect);

        // add tag if applicable
        if (effectRuntime.Tag.ItemTag != ItemEffectTagId.None)
        {
            var tagIndex = (int)effectRuntime.Tag.ItemTag;
            // add EffectTag component to effect
            effect.Add(new ItemEffectTag
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
    }

    public void UnregisterEffect(GameState state, Entity effect, EffectRuntime effectRuntime)
    {
        if (!effect.IsAlive()) // don't use helpers, effect with ExpiredTag should be removable
            return;

        // remove the effect from the target's ItemEffects
        ref var itemEffects = ref _target.Get<ItemEffects>();
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

        // destroy effect
        state.World.Destroy(effect);
    }
    public void MarkAsDirtyIfNeeded(Entity effect)
    {
        // TODO:
        // ItemStatModifiers: for item specific modifiers
        // ItemResourceModifiers: for item with a dedicated resource
        // ItemResourceRegenModifiers: for item with a dedicated resource

        // if item is worn, check character stat/resource/resource regen modifiers
        ref var equipped = ref _target.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            var wearer = equipped.Wearer;
            if (wearer.IsAlive())
            {
                // if effect has StatModifiers
                // flag the wearer's stats as dirty so they will be recalculated without this effect
                if (effect.Has<CharacterStatModifiers>() && !wearer.Has<DirtyStats>())
                    wearer.Add<DirtyStats>();

                // if effect has ResourceModifiers
                // flag the wearer's resources as dirty so they will be recalculated without this effect
                if (effect.Has<CharacterResourceModifiers<HealthModifier>>() && !wearer.Has<DirtyHealth>())
                    wearer.Add<DirtyHealth>();
                if (effect.Has<CharacterResourceModifiers<ManaModifier>>() && !wearer.Has<DirtyMana>())
                    wearer.Add<DirtyMana>();
                if (effect.Has<CharacterResourceModifiers<EnergyModifier>>() && !wearer.Has<DirtyEnergy>())
                    wearer.Add<DirtyEnergy>();
                if (effect.Has<CharacterResourceModifiers<RageModifier>>() && !wearer.Has<DirtyRage>())
                    wearer.Add<DirtyRage>();

                // if effect has ResourceRegebModifiers
                // flag the wearer's resource regens as dirty so they will be recalculated without this effect
                if (effect.Has<CharacterResourceRegenModifiers<HealthRegenModifier>>() && !wearer.Has<DirtyHealthRegen>())
                    wearer.Add<DirtyHealthRegen>();
                if (effect.Has<CharacterResourceRegenModifiers<ManaRegenModifier>>() && !wearer.Has<DirtyManaRegen>())
                    wearer.Add<DirtyManaRegen>();
                if (effect.Has<CharacterResourceRegenModifiers<EnergyModifier>>() && !wearer.Has<DirtyEnergyRegen>())
                    wearer.Add<DirtyEnergyRegen>();
                if (effect.Has<CharacterResourceRegenModifiers<RageDecayModifier>>() && !wearer.Has<DirtyRageDecay>())
                    wearer.Add<DirtyRageDecay>();
            }
        }
    }

    public EffectValuesSnapshot CreateSnapshot()
    {
        // snapshot target values
        return new EffectValuesSnapshot
        {
            ItemLevel = _target.Get<Level>().Value,
            // TODO: other values depending on item type
        };
    }
}
