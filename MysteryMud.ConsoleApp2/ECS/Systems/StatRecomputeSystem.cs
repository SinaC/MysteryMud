using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp2.ECS.Components;
using MysteryMud.ConsoleApp2.ECS.Components.Characters;
using MysteryMud.ConsoleApp2.ECS.Components.Effects;

namespace MysteryMud.ConsoleApp2.ECS.Systems;

public static class StatRecomputeSystem
{
    public static void Update(World world)
    {
        var query = new QueryDescription()
            .WithAll<BaseStats, StatsDirty>();
        world.Query(in query, (Entity e, ref BaseStats baseStats) =>
        {
            FinalStats final = new FinalStats
            {
                Strength = baseStats.Strength,
                Dexterity = baseStats.Dexterity,
                Constitution = baseStats.Constitution,
            };

            var modQuery = new QueryDescription().WithAll<StatModifier, EffectTarget>();
            world.Query(in modQuery, (ref StatModifier mod, ref EffectTarget target) =>
            {
                if (target.Target != e)
                    return;

                if (mod.Stat == StatType.Strength)
                    final.Strength += mod.Amount;

                if (mod.Stat == StatType.Dexterity)
                    final.Dexterity += mod.Amount;

                if (mod.Stat == StatType.Constitution)
                    final.Constitution += mod.Amount;
            });

            var extraQuery = new QueryDescription().WithAll<ExtraAttack, EffectTarget>();
            world.Query(in extraQuery, (ref ExtraAttack ea, ref EffectTarget target) =>
            {
                if (target.Target == e)
                    final.ExtraAttacks += ea.Amount;
            });

            e.Set(final);
            e.Remove<StatsDirty>();
        });
    }
}
