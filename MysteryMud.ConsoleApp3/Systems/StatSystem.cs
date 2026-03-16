using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Systems;

class StatSystem
{
    public static void Recalculate(World world)
    {
        var query = new QueryDescription()
                .WithAll<BaseStats, EffectiveStats, DirtyStats>()
                .WithNone<DeadTag>();
        world.Query(query, (Entity character,
                     ref BaseStats baseStats,
                     ref EffectiveStats effectiveStats,
                     ref DirtyStats dirty) =>
        {
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

                ref var effects = ref character.Get<CharacterEffects>();
                foreach (var effect in effects.Effects)
                {
                    ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
                    if (hasStatModifiers)
                    {
                        foreach (var mod in statModifiers.Values)
                        {
                            if (mod.Stat != stat) // TODO: optimize by only iterating modifiers for this stat, instead of all modifiers for all stats -> index modifiers by stat in the StatModifiers component
                                continue;
                            switch (mod.Type)
                            {
                                case ModifierType.Flat:
                                    flat += mod.Value;
                                    break;
                                case ModifierType.AddPercent:
                                    percent += mod.Value;
                                    break;
                                case ModifierType.Multiply:
                                    multiply *= mod.Value;
                                    break;
                                case ModifierType.Override: // what if multiple overrides? for now, just take the last one, but maybe we should prioritize by source (e.g. gear overrides > buff overrides > debuff overrides) or something like that
                                    overriding = mod.Value;
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
