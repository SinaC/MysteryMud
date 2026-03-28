using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.OldSystems;

public static class CombatSystem
{
    public static void Process(SystemContext ctx, GameState state)
    {
        var query = new QueryDescription()
            .WithAll<CombatState, EffectiveStats>()
            .WithNone<Dead>();
        state.World.Query(query, (Entity actor, ref CombatState combat, ref EffectiveStats stats) =>
        {
            // TODO: if NPC, SelectTarget with highest threat

            var target = combat.Target;
            if (target.Has<Dead>())
            {
                // Target is gone, exit combat
                actor.Remove<CombatState>();
                return;
                // TODO: Alternatively, could try to select new target here instead of exiting combat
            }

            if (combat.RoundDelay > 0)
            {
                combat.RoundDelay--;
                return;
            }

            // Determine number of attacks (multi-hit)
            int hits = 2; // GetMultiHitCount(actor);

            for (int i = 0; i < hits; i++)
            {
                // Stop if target was killed by previous hit
                if (target.Has<Dead>())
                    break;

                bool targetAlive = ResolveAttack(ctx, state.World, actor, target, stats);

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
    private static bool ResolveAttack(SystemContext ctx, World world, Entity attacker, Entity target, EffectiveStats stats)
    {
        var targetStats = target.Get<EffectiveStats>();
        int attackRoll = stats.Values[StatTypes.HitRoll] + Random.Shared.Next(1, 20);
        int defenseRoll = targetStats.Values[StatTypes.Armor] + Random.Shared.Next(1, 20);

        if (attackRoll >= defenseRoll)
        {
            var damage = stats.Values[StatTypes.DamRoll] + Random.Shared.Next(1, 6); // TODO: calculate damage based on weapon, skills, etc.
            var damageType = DamageTypes.Physical; // TODO: determine damage type based on weapon, skills, etc.

            var result = DamageSystem.ApplyDamage(ctx, target, damage, damageType, attacker);

            return result != DamageSystem.ApplyDamageResult.Killed && result != DamageSystem.ApplyDamageResult.Dead;
        }
        else
        {
            ctx.Msg.ToAll(attacker).Act("{0} miss{0:v} {1}.").With(attacker, target);

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