using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class EffectiveCharacterStatsSystem
{
    private static readonly CharacterStatKind[] _allStats = Enum.GetValues<CharacterStatKind>()
        .Take((int)CharacterStatKind.Count) // Count is used by to InlineArray attribute
        .ToArray();

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
            ref var equipment = ref character.Get<Equipment>();

            // TODO: optimize by only recalculating stats that are dirty, instead of all stats for the character. this would require tracking which stats are dirty, either by having a separate DirtyStats component for each stat, or by having a bitfield in the DirtyStats component that tracks which stats are dirty
            // TODO: optimize by only iterating modifiers for this stat, instead of all modifiers for all stats -> index modifiers by stat in the StatModifiers component
            foreach (var stat in _allStats)
            {
                // apply base stat
                var baseValue = baseStats.Values[stat];

                decimal flat = 0, percent = 0, multiply = 1;
                decimal? overriding = null;

                // apply modifiers from equipment
                foreach (var (slot, equipedItem) in equipment.Slots)
                {
                    ref var itemEffects = ref equipedItem.Get<ItemEffects>();
                    var characterModifiersFromItem = ModifierPipeline.CalculateModifiers<CharacterStatModifiers, CharacterStatModifier>(itemEffects, x => x.Stat == stat, x => x.Values, x => x.Modifier, x => x.Value);
                    flat += characterModifiersFromItem.flat;
                    percent += characterModifiersFromItem.percent;
                    multiply *= characterModifiersFromItem.multiply;
                    overriding ??= characterModifiersFromItem.overriding;
                }

                //  this will allow to remove: x => x.Stat == stat
                // apply modifiers from effects
                var characterModifiersFromCharacter = ModifierPipeline.CalculateModifiers<CharacterStatModifiers, CharacterStatModifier>(characterEffects, x => x.Stat == stat, x => x.Values, x => x.Modifier, x => x.Value);
                flat += characterModifiersFromCharacter.flat;
                percent += characterModifiersFromCharacter.percent;
                multiply *= characterModifiersFromCharacter.multiply;
                overriding ??= characterModifiersFromCharacter.overriding;

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
