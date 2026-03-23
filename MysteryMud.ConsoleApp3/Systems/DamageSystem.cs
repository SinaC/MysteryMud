using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using MysteryMud.ConsoleApp3.Domain.Factories;
using MysteryMud.ConsoleApp3.Simulation.Calculators;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DamageSystem
{
    public static ApplyDamageResult ApplyDamage(SystemContext ctx, Entity target, int damageAmount, DamageType damageType, Entity source)
    {
        if (target.Has<Dead>())
            return ApplyDamageResult.Dead; // can't damage something that's already dead

        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return ApplyDamageResult.CannotBeDamaged; // can't damage something without health

        // apply damage type modifiers, resistances, vulnerabilities, etc.
        var modifiedDamage = DamageCalculator.ModifyDamage(target, damageAmount, damageType, source);

        return ApplyDamage(ctx, target, ref health, modifiedDamage, damageType, source);
    }

    private static ApplyDamageResult ApplyDamage(SystemContext ctx, Entity target, ref Health health, int damageAmount, DamageType damageType, Entity source) // TODO: optional source
    {
        // apply damage and check if killed
        health.Current -= damageAmount;

        ctx.Log.Damage("Applying damage from {sourceName} to {targetName} with amount {damage}. Current health: {health.Current}/{health.Max}", source.DebugName, target.DebugName, damageAmount, health.Current, health.Max);

        ctx.MessageBus.Publish(source, $"%GYou deal %r{damageAmount}%g damage to {target.DisplayName}.%x");
        ctx.MessageBus.Publish(target, $"{source.DisplayName} deals {damageAmount} damage to you.");

        if (health.Current <= 0)
        {
            ctx.Log.Damage("Target {targetName} killed by {sourceName}", target.DebugName, source.DebugName);

            ctx.MessageBus.Publish(source, $"%R{target.DisplayName} is dead.%x");
            ctx.MessageBus.Publish(target, $"You are dead.");

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
                RespawnRoom = RoomFactory.RespawnRoomEntity,
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
