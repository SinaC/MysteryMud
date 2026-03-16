using Arch.Core;
using MysteryMud.ConsoleApp.Components;
using MysteryMud.ConsoleApp.Components.Effects;
using MysteryMud.ConsoleApp.Events;

namespace MysteryMud.ConsoleApp.Systems;

static class SpellSystem
{
    public static void CastGiantStrength(Entity target, CommandBuffer cmd)
    {
        cmd.Add(w =>
        {
            w.Add(target, new GiantStrength { Bonus = 4, Duration = 60 });
            w.Get<StatsDirty>(target).Value = true;
        });
    }

    public static void CastHaste(Entity target, CommandBuffer cmd)
    {
        cmd.Add(w =>
        {
            w.Add(target, new Haste { Duration = 30 });
            w.Get<StatsDirty>(target).Value = true;
        });
    }

    public static void CastSanctuary(Entity target, CommandBuffer cmd)
    {
        cmd.Add(w => w.Add(target, new Sanctuary { Duration = 20 }));
    }

    public static void CastPoison(Entity target, CommandBuffer cmd)
    {
        cmd.Add(world =>
        {
            world.Add(target, new DamageOverTime
            {
                Damage = 5,
                TickInterval = 3,
                Duration = 30
            });
        });
    }

    public static void CastFireball(World world, Entity caster, CombatEventQueue queue)
    {
        var room = world.Get<InRoom>(caster).Room;
        var entities = world.Get<RoomEntities>(room).Entities;

        foreach (var target in entities)
        {
            if (target == caster || !world.Has<Health>(target))
                continue;

            queue.DamageEvents.Add(new DamageEvent
            {
                Source = caster,
                Target = target,
                Amount = 30,
                IsSpell = true
            });
        }
    }

    public static void CastPoisonCloud(World world, Entity caster, CombatEventQueue queue)
    {
        var room = world.Get<InRoom>(caster).Room;
        var entities = world.Get<RoomEntities>(room).Entities;

        foreach (var target in entities)
        {
            if (!world.Has<Health>(target))
                continue;

            queue.DamageEvents.Add(new DamageEvent
            {
                Source = caster,
                Target = target,
                Amount = 0
            });

            ApplyBuff(world, target, new DamageOverTime
            {
                Damage = 4,
                TickInterval = 2,
                Duration = 20
            }, StackPolicy.Stack);
        }
    }

    public static void ApplyBuff<T>(World world, Entity target, T buff, StackPolicy policy)
        where T : struct
    {
        if (world.Has<T>(target))
        {
            switch (policy)
            {
                case StackPolicy.RefreshDuration:
                    world.Set(target, buff);
                    break;
                case StackPolicy.Replace:
                    world.Set(target, buff);
                    break;
                case StackPolicy.Stack:
                    world.Add(target, buff); // creates multiple DoT components
                    break;
            }
        }
        else
        {
            world.Add(target, buff);
        }
    }
}
