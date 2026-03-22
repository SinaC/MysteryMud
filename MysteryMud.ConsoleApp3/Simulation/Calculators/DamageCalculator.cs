using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Simulation.Calculators;

public static class DamageCalculator
{
    public static ApplyDamageResult ApplyDamage(Entity target, int damageAmount, DamageType damageType, Entity source)
    {
        if (target.Has<Dead>())
            return ApplyDamageResult.Dead; // can't damage something that's already dead

        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return ApplyDamageResult.CannotBeDamaged; // can't damage something without health

        // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.

        //ref var health = ref target.Get<Health>();
        return ApplyDamage(target, ref health, damageAmount, source);
    }

    private static ApplyDamageResult ApplyDamage(Entity target, ref Health health, int damageAmount, Entity source) // TODO: optional source
    {
        health.Current -= damageAmount;

        Logger.Logger.Damage.Apply(source, target, damageAmount, ref health);

        MessageSystem.Send(source, $"%GYou deal %r{damageAmount}%g damage to {target.DisplayName}.%x");
        MessageSystem.Send(target, $"{source.DisplayName} deals {damageAmount} damage to you.");

        if (health.Current <= 0)
        {
            Logger.Logger.Damage.TargetKilled(source, target);

            MessageSystem.Send(source, $"%R{target.DisplayName} is dead.%x");
            MessageSystem.Send(source, $"You are dead.");

            AddTags(target, source);
            return ApplyDamageResult.Killed;
        }

        AggroCalculator.AddAggro(target, source, damageAmount); // healing generates some aggro, but less than damage

        return ApplyDamageResult.Damaged;
    }

    private static void AddTags(Entity victim, Entity killer)
    {
        victim.Add(new Dead
        {
            Killer = killer
        }); // mark as dead
        // player will respawn, NPCs will be cleaned up by CleanupSystem
        if (victim.Has<PlayerTag>())
            victim.Add(new RespawnState
            {
                RespawnRoom = WorldFactory.RespawnRoomEntity,
                Killer = killer
            });
    }

    public enum ApplyDamageResult
    {
        Dead,
        Killed,
        CannotBeDamaged,
        Damaged,
    }
}
