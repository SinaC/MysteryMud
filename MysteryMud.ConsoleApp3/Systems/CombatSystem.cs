using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

class CombatSystem
{
    public static void Process(World world)
    {
        var query = new QueryDescription()
            .WithAll<CombatState, EffectiveStats>()
            .WithNone<DeadTag>();
        world.Query(query, (Entity actor, ref CombatState combat, ref EffectiveStats stats) =>
        {
            // TODO: if NPC, SelectTarget with highest threat

            if (combat.RoundDelay > 0)
            {
                combat.RoundDelay--;
                return;
            }

            var target = combat.Target;
            if (target.Has<DeadTag>())
            {
                // Target is gone, exit combat
                actor.Remove<CombatState>();
                return;
                // TODO: Alternatively, could try to select new target here instead of exiting combat
            }

            // Determine number of attacks (multi-hit)
            int hits = 2; // GetMultiHitCount(actor);

            for (int i = 0; i < hits; i++)
            {
                // Stop if target was killed by previous hit
                if (target.Has<DeadTag>())
                    break;

                bool targetAlive = ResolveAttack(world, actor, target, stats);

                //// Trigger weapon proc (only if target still alive)
                //if (targetAlive)
                //    TriggerWeaponProc(world, actor, target);

                if (!targetAlive)
                    break; // stop multi-hit if target died
            }

            // Apply lag before next attack
            combat.RoundDelay = 2; // example: 2 ticks
        });
    }

    // Resolve a single attack and immediately apply damage
    private static bool ResolveAttack(World world, Entity attacker, Entity target, EffectiveStats stats)
    {
        var targetStats = target.Get<EffectiveStats>();
        int attackRoll = stats.HitRoll + Random.Shared.Next(1, 20);
        int defenseRoll = targetStats.Armor + Random.Shared.Next(1, 20);

        if (attackRoll >= defenseRoll)
        {
            int damage = stats.DamRoll + Random.Shared.Next(1, 6);

            return DamageSystem.ApplyDamage(world, target, damage, attacker);
        }
        else
        {
            MessageSystem.SendMessage(attacker, $"You miss {target.DisplayName}.");
            MessageSystem.SendMessage(target, $"{attacker.DisplayName} misses you.");
            //TODO: log Console.WriteLine($"{attacker.DisplayName} misses {target.DisplayName}.");
            return true;
        }
    }

    public static Entity SelectTarget(ThreatTable table)
    {
        Entity best = Entity.Null;
        int highest = 0;

        foreach (var kv in table.Threat)
        {
            if (kv.Value > highest)
            {
                highest = kv.Value;
                best = kv.Key;
            }
        }

        return best;
    }
}