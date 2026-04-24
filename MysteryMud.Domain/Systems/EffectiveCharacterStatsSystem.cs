using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class EffectiveCharacterStatsSystem
{
    private static readonly CharacterStatKind[] _allStats = Enum.GetValues<CharacterStatKind>()
        .Take((int)CharacterStatKind.Count) // Count is used by to InlineArray attribute
        .ToArray();

    private readonly World _world;

    public EffectiveCharacterStatsSystem(World world)
    {
        _world = world;
    }

    private static readonly QueryDescription _hasDirtyStatsQueryDesc = new QueryDescription()
        .WithAll<BaseStats, EffectiveStats, DirtyStats, CharacterEffects, Equipment>()
        .WithNone<Dead>();

    public void Tick(GameState state)
    {
        _world.Query(_hasDirtyStatsQueryDesc, (EntityId character,
             ref BaseStats baseStats,
             ref EffectiveStats effectiveStats,
             ref DirtyStats _,
             ref CharacterEffects characterEffects,
             ref Equipment equipment) =>
        {
            // accumulators indexed by stat — stack alloc, no heap pressure
            var statCount = _allStats.Length;
            Span<decimal> flat = stackalloc decimal[statCount];
            Span<decimal> percent = stackalloc decimal[statCount];
            Span<decimal> multiply = stackalloc decimal[statCount];
            Span<decimal> overriding = stackalloc decimal[statCount];
            Span<bool> hasOverriding = stackalloc bool[statCount];
            multiply.Fill(1m);

            // single pass over equipment modifiers — O(slots × modifiers_per_item)
            foreach (var (slot, equippedItem) in equipment.Slots)
            {
                ref var itemEffects = ref _world.Get<ItemEffects>(equippedItem);
                ModifierPipeline.AccumulateModifiers<CharacterStatModifiers, CharacterStatModifier>(
                    _world,
                    itemEffects.Data.Effects,
                    x => x.Stat,           // route modifier to correct stat bucket
                    x => x.Values,
                    x => x.Modifier,
                    x => x.Value,
                    flat, percent, multiply, overriding, hasOverriding);
            }

            // single pass over all character effects
            ModifierPipeline.AccumulateModifiers<CharacterStatModifiers, CharacterStatModifier>(
                _world,
                characterEffects.Data.Effects,
                x => x.Stat,
                x => x.Values,
                x => x.Modifier,
                x => x.Value,
                flat, percent, multiply, overriding, hasOverriding);

            // now apply — one pass over stats, no modifier scanning
            foreach (var stat in _allStats)
            {
                var i = (int)stat;
                var baseValue = baseStats.Values[stat];
                var rawValue = hasOverriding[i]
                    ? overriding[i]
                    : ((baseValue + flat[i]) * (100 + percent[i]) * multiply[i] / 100);

                effectiveStats.Values[stat] = (int)Math.Round(rawValue, MidpointRounding.AwayFromZero);
            }

            // mark as clean
            _world.Remove<DirtyStats>(character);
        });
    }
}
