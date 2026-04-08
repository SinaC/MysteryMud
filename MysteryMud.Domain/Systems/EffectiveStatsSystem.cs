using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class EffectiveStatsSystem
{
    private static readonly StatKind[] _allStats = Enum.GetValues<StatKind>();

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
            ref var characterEffects = ref character.Get<CharacterEffects>();

            // TODO: optimize by only recalculating stats that are dirty, instead of all stats for the character. this would require tracking which stats are dirty, either by having a separate DirtyStats component for each stat, or by having a bitfield in the DirtyStats component that tracks which stats are dirty
            foreach (var stat in _allStats)
            {
                // apply base stat
                var baseValue = baseStats.Values[stat];

                // TODO: apply modifiers from equipment

                // TODO: optimize by only iterating modifiers for this stat, instead of all modifiers for all stats -> index modifiers by stat in the StatModifiers component
                //  this will allow to remove: x => x.Stat == stat
                // apply modifiers from effects
                var (flat, percent, multiply, overriding) = ModifierPipeline.CalculateModifiers<StatModifiers, StatModifier>(character, x => x.Stat == stat, x => x.Values, x => x.Modifier, x => x.Value);

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
