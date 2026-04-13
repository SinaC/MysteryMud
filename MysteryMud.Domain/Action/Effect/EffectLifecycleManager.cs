using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect;

public class EffectLifecycleManager : IEffectLifecycleManager
{
    public void RemoveEffect(GameState state, Entity effect)
    {
        if (!effect.IsAlive()) // don't use helpers, effect with ExpiredTag should be removable
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive())
            return;

        // remove the effect from the target's CharacterEffects
        ref var characterEffects = ref effectInstance.Target.Get<CharacterEffects>();
        characterEffects.Effects.Remove(effect);
        // remove tag if applicable
        if (effectInstance.EffectRuntime != null)
        {
            if (effectInstance.EffectRuntime.Tag != EffectTagId.None)
            {
                int tagIndex = (int)effectInstance.EffectRuntime.Tag;
                var effectsByTag = characterEffects.EffectsByTag[tagIndex];
                if (effectsByTag != null)
                {
                    effectsByTag.Remove(effect);
                    if (effectsByTag.Count == 0)
                        characterEffects.ActiveTags &= ~(1UL << tagIndex); // remove tag from active tags when last effect on that tag is removed
                }
            }
        }

        // if effect has StatModifiers
        // flag the target's stats as dirty so they will be recalculated without this effect
        if (effect.Has<StatModifiers>() && !effectInstance.Target.Has<DirtyStats>())
            effectInstance.Target.Add<DirtyStats>();

        // if effect has ResourceModifiers
        // flag the target's resources as dirty so they will be recalculated without this effect
        if (effect.Has<ResourceModifiers<HealthModifier>>() && !effectInstance.Target.Has<DirtyHealth>())
            effectInstance.Target.Add<DirtyHealth>();
        if (effect.Has<ResourceModifiers<ManaModifier>>() && !effectInstance.Target.Has<DirtyMana>())
            effectInstance.Target.Add<DirtyMana>();
        if (effect.Has<ResourceModifiers<EnergyModifier>>() && !effectInstance.Target.Has<DirtyEnergy>())
            effectInstance.Target.Add<DirtyEnergy>();
        if (effect.Has<ResourceModifiers<RageModifier>>() && !effectInstance.Target.Has<DirtyRage>())
            effectInstance.Target.Add<DirtyRage>();

        // destroy effect
        state.World.Destroy(effect);
    }

}
