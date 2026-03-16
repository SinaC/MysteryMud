using Arch.Core;
using MysteryMud.ConsoleApp.Components;
using MysteryMud.ConsoleApp.Components.Effects;

namespace MysteryMud.ConsoleApp.Systems;

static class StatSystem
{
    static QueryDescription statQuery =
        new QueryDescription().WithAll<BaseStats, EffectiveStats, StatsDirty>();

    public static void Run(World world)
    {
        world.Query(statQuery, (Entity entity,
                               ref BaseStats baseStats,
                               ref EffectiveStats effective,
                               ref StatsDirty dirty) =>
        {
            if (!dirty.Value)
                return;

            effective.Strength = baseStats.Strength;
            effective.Agility = baseStats.Agility;
            effective.Vitality = baseStats.Vitality;
            effective.Attacks = 1;

            ApplyEquipment(world, entity, ref effective);
            ApplyBuffs(world, entity, ref effective);

            dirty.Value = false;
        });
    }

    static void ApplyEquipment(World world, Entity entity, ref EffectiveStats stats)
    {
        if (!world.Has<Equipment>(entity))
            return;

        var eq = world.Get<Equipment>(entity);

        foreach (var slot in eq.Slots.Values)
        {
            if (!world.Has<ItemStats>(slot))
                continue;

            var item = world.Get<ItemStats>(slot);

            stats.Strength += item.Strength;
            stats.Agility += item.Agility;
            stats.Vitality += item.Vitality;
        }
    }

    static void ApplyBuffs(World world, Entity entity, ref EffectiveStats stats)
    {
        if (world.Has<GiantStrength>(entity))
        {
            var buff = world.Get<GiantStrength>(entity);
            stats.Strength += buff.Bonus;
        }

        if (world.Has<Haste>(entity))
        {
            stats.Attacks += 1;
        }
    }
}
