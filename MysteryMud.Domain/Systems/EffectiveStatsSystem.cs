using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class EffectiveStatsSystem
{
    public void Tick(GameState state)
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
            foreach (var stat in Enum.GetValues<StatKind>())
            {
                // apply base stat
                var baseValue = baseStats.Values[stat];

                // TODO: apply modifiers from equipment

                // apply modifiers from effects
                var flat = 0m;
                var percent = 0m;
                var multiply = 1m;
                var overriding = (decimal?)null;

                ref var characterEffects = ref character.Get<CharacterEffects>();
                foreach (var effect in characterEffects.Effects)
                {
                    if (!EffectHelpers.IsAlive(effect))
                        continue;

                    ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
                    if (!hasStatModifiers)
                        continue;

                    ref var effectInstance = ref effect.Get<EffectInstance>();
                    var stackCount = effectInstance.StackCount;
                    foreach (var modifier in statModifiers.Values)
                    {
                        if (modifier.Stat != stat) // TODO: optimize by only iterating modifiers for this stat, instead of all modifiers for all stats -> index modifiers by stat in the StatModifiers component
                            continue;
                        var modifierValue = modifier.Value * stackCount;
                        switch (modifier.Modifier)
                        {
                            case ModifierKind.Flat:
                                flat += modifierValue;
                                break;
                            case ModifierKind.AddPercent:
                                percent += modifierValue;
                                break;
                            case ModifierKind.Multiply:
                                multiply *= modifierValue;
                                break;
                            case ModifierKind.Override: // what if multiple overrides? for now, just take the last one, but maybe we should prioritize by source (e.g. gear overrides > buff overrides > debuff overrides) or something like that
                                overriding = modifierValue;
                                break;
                        }
                    }
                }

                var rawValue = overriding ?? ((baseValue + flat) * (100 + percent) * multiply / 100);

                // TODO: capping
                var finalValue = rawValue;

                effectiveStats.Values[stat] = (int)Math.Round(finalValue, MidpointRounding.AwayFromZero);
            }

            // mark as clean
            character.Remove<DirtyStats>();
        });
    }
}
