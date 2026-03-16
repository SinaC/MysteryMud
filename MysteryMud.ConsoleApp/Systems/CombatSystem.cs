using Arch.Core;
using MysteryMud.ConsoleApp.Components;
using MysteryMud.ConsoleApp.Events;

namespace MysteryMud.ConsoleApp.Systems;

static class CombatSystem
{
    static QueryDescription combatQuery =
         new QueryDescription().WithAll<Attack, Target, EffectiveStats>();

    public static void Run(World world, CombatEventQueue queue)
    {
        world.Query(combatQuery, (Entity attacker,
                            ref Attack atk,
                            ref Target target,
                            ref EffectiveStats stats) =>
        {
            if (!world.IsAlive(target.Value))
                return;

            for (int i = 0; i < stats.Attacks; i++)
                QueueMeleeAttack(world, queue, attacker, target.Value, atk.Damage);
        });
    }

    static void QueueMeleeAttack(World world, CombatEventQueue queue, Entity attacker, Entity target, int baseDamage)
    {
        ref var eff = ref world.Get<EffectiveStats>(attacker);
        int damage = baseDamage + eff.Strength;

        queue.DamageEvents.Add(new DamageEvent
        {
            Source = attacker,
            Target = target,
            Amount = damage,
            IsSpell = false,
            IsCritical = false
        });
    }
}
