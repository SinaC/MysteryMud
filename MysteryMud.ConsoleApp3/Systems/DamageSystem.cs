using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Simulation.Calculators;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DamageSystem
{
    public static ApplyDamageResult ApplyDamage(Entity target, int damageAmount, DamageType damageType, Entity source)
    {
        if (target.Has<Dead>())
            return ApplyDamageResult.Dead; // can't damage something that's already dead

        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return ApplyDamageResult.CannotBeDamaged; // can't damage something without health

        // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.
        var modifiedDamage = DamageCalculator.ModifyDamage(target, damageAmount, damageType, source);

        //ref var health = ref target.Get<Health>();
        return ApplyDamage(target, ref health, modifiedDamage, damageType, source);
    }

    private static ApplyDamageResult ApplyDamage(Entity target, ref Health health, int damageAmount, DamageType damageType, Entity source) // TODO: optional source
    {
        // apply damage and check if killed
        health.Current -= damageAmount;

        Logger.Logger.Damage.Apply(source, target, damageAmount, ref health);

        MessageBus.Publish(source, $"%GYou deal %r{damageAmount}%g damage to {target.DisplayName}.%x");
        MessageBus.Publish(target, $"{source.DisplayName} deals {damageAmount} damage to you.");

        if (health.Current <= 0)
        {
            Logger.Logger.Damage.TargetKilled(source, target);

            MessageBus.Publish(source, $"%R{target.DisplayName} is dead.%x");
            MessageBus.Publish(target, $"You are dead.");

            AddKilledTags(target, source);
            return ApplyDamageResult.Killed;
        }

        // generate aggro
        var aggro = AggroCalculator.CalculateDamageAggro(target, source, damageAmount, damageType);
        AggroSystem.AddAggro(target, source, aggro);

        return ApplyDamageResult.Damaged;
    }

    private static void AddKilledTags(Entity victim, Entity killer)
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
