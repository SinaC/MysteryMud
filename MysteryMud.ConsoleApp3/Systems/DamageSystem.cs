using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DamageSystem
{
    public static bool ApplyDamage(World world, Entity target, int amount, Entity source)
    {
        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return false; // can't damage something without health

        //ref var health = ref target.Get<Health>();
        return ApplyDamage(world, target, ref health, amount, source);
    }

    private static bool ApplyDamage(World world, Entity target, ref Health health, int amount, Entity source)
    {
        health.Current -= amount;
        //TODO: log MessageSystem.SendMessage(actor, $"{target.GDisplayName} takes {amount} damage! (HP: {health.Current})");
        MessageSystem.Send(source, $"%GYou deal %r{amount}%g damage to {target.DisplayName}.%x");
        MessageSystem.Send(target, $"{source.DisplayName} deals {amount} damage to you.");

        if (health.Current <= 0)
        {
            DeathSystem.Die(world, target, source);
            return false;
        }

        AddAggro(target, source, amount);

        return true;
    }

    private static void AddAggro(Entity target, Entity source, int amount)
    {
        if (source == Entity.Null)
            return;
        ref var threatTable = ref target.TryGetRef<ThreatTable>(out var hasThreat);
        if (!hasThreat)
            return;
        if (threatTable.Threat.ContainsKey(source))
            threatTable.Threat[source] += amount;
        else
            threatTable.Threat.Add(source, amount);
    }
}
