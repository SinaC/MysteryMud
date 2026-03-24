using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Data.Enums;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;

namespace MysteryMud.Application.Systems;

public static class StatSystem
{
    public static void Process(GameState state)
    {
        var query = new QueryDescription()
                .WithAll<BaseStats, EffectiveStats, DirtyStats>()
                .WithNone<Dead>();
        state.World.Query(query, (Entity character,
                     ref BaseStats baseStats,
                     ref EffectiveStats effectiveStats,
                     ref DirtyStats dirty) =>
        {
            // no modifiers, so just copy base stats to effective stats
            effectiveStats.Level = baseStats.Level;
            effectiveStats.Experience = baseStats.Experience;

            // TODO: optimize by only recalculating stats that are dirty, instead of all stats for the character. this would require tracking which stats are dirty, either by having a separate DirtyStats component for each stat, or by having a bitfield in the DirtyStats component that tracks which stats are dirty
            foreach (var stat in Enum.GetValues<StatType>())
            {
                // apply base stat
                var baseValue = baseStats.Values[stat];

                // TODO: apply modifiers from equipment

                // apply modifiers from effects
                var flat = 0;
                var percent = 0;
                var multiply = 1;
                var overriding = (int?)null;

                ref var characterEffects = ref character.Get<CharacterEffects>();
                foreach (var effect in characterEffects.Effects)
                {
                    ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
                    if (hasStatModifiers)
                    {
                        ref var effectInstance = ref effect.Get<EffectInstance>();

                        foreach (var modifier in statModifiers.Values)
                        {
                            if (modifier.Stat != stat) // TODO: optimize by only iterating modifiers for this stat, instead of all modifiers for all stats -> index modifiers by stat in the StatModifiers component
                                continue;
                            var modifierValue = modifier.Value * effectInstance.StackCount;
                            switch (modifier.Type)
                            {
                                case ModifierType.Flat:
                                    flat += modifierValue;
                                    break;
                                case ModifierType.AddPercent:
                                    percent += modifierValue;
                                    break;
                                case ModifierType.Multiply:
                                    multiply *= modifierValue;
                                    break;
                                case ModifierType.Override: // what if multiple overrides? for now, just take the last one, but maybe we should prioritize by source (e.g. gear overrides > buff overrides > debuff overrides) or something like that
                                    overriding = modifierValue;
                                    break;
                            }
                        }
                    }
                }

                var finalValue = overriding ?? ((baseValue + flat) * (100 + percent) * multiply / 100);
                effectiveStats.Values[stat] = finalValue;
            }

            // mark as clean
            character.Remove<DirtyStats>();
        });
    }
}
