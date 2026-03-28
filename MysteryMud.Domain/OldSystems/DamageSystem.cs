using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Calculators;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Factories;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.OldSystems;

public static class DamageSystem
{
    public static ApplyDamageResult ApplyDamage(SystemContext ctx, Entity target, int damageAmount, DamageTypes damageType, Entity source)
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

    private static ApplyDamageResult ApplyDamage(SystemContext ctx, Entity target, ref Health health, int damageAmount, DamageTypes damageType, Entity source) // TODO: optional source
    {
        // apply damage and check if killed
        health.Current -= damageAmount;

        ctx.Log.LogInformation(LogEvents.Damage,"Applying damage from {sourceName} to {targetName} with amount {damage}. Current health: {health.Current}/{health.Max}", source.DebugName, target.DebugName, damageAmount, health.Current, health.Max);

        ctx.Msg.ToAll(source).Act("%G{0} deal{0:v} %r{1}%g damage to {2}.%x").With(source, damageAmount, target);

        if (health.Current <= 0)
        {
            ctx.Log.LogInformation(LogEvents.Damage,"Target {targetName} killed by {sourceName}", target.DebugName, source.DebugName);

            ctx.Msg.To(target).Send("%RYou have been KILLED%x");
            ctx.Msg.ToRoom(target).Act("{0} is dead").With(target);

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
