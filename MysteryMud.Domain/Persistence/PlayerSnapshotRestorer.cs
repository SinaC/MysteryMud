using Arch.Core;
using MysteryMud.Core.Persistence;
using MysteryMud.Core.Persistence.Snapshots;

namespace MysteryMud.Domain.Persistence;

public sealed class PlayerSnapshotRestorer : ISnapshotRestorer
{
    public void Restore(World world, Entity entity, PlayerSnapshot snap, long currentTick)
    {
        /*
        // Core
        world.Add(entity, new Name { Value = snap.Name });
        world.Add(entity, new Level { Value = snap.Level });
        world.Add(entity, new Location(snap.LocationKey));
        world.Add(entity, new Position(snap.Position));
        world.Add(entity, new Form(snap.Form));
        world.Add(entity, new Progression(snap.TotalXp));
        world.Add(entity, new AutoBehaviour(snap.AutoBehavior));
        world.Add(entity, new PlayerDbId(snap.Id));

        // Stats
        var statValues = snap.Stats
            .Select(s => (s.Stat, s.BaseValue, s.EffValue))
            .ToList();
        world.Add(entity, new BaseStats(statValues));

        // Resources
        foreach (var r in snap.Resources)
        {
            if (r.Resource == "Health")
                world.Add(entity, new Health(r.Current, r.Maximum, r.BaseRegen, r.CurrentRegen));
            // else if (r.Resource == "Mana") ...
        }

        // Effects — rebase tick offsets to current tick
        var liveEffects = snap.Effects
            .Select(e => new LiveEffect(
                e.EffectId,
                e.Tags,
                e.TickRate,
                TickHelper.FromNextTickOffset(e.NextTickOffset, currentTick),
                TickHelper.FromExpirationOffset(e.ExpirationRemaining, currentTick),
                e.ParamsJson))
            .ToList();
        world.Add(entity, new CharacterEffects(liveEffects));

        // Abilities — rebase cooldowns
        var liveAbilities = snap.Abilities
            .Select(a => new LiveAbility(
                a.AbilityKey,
                a.ClassKey,
                a.LearnedPercent,
                a.LearnedLevel,
                a.MasteryTier,
                TickHelper.FromCooldownRemaining(a.CooldownTicksRemaining, currentTick),
                a.Charges))
            .ToList();
        world.Add(entity, new LearnedAbilities(liveAbilities));

        // Optional
        if (snap.OptionalJson != null)
            RestoreOptional(world, entity, snap.OptionalJson);

        // Items are spawned separately by an ItemSpawnService using the ItemSnapshot list.
        // Do not add inventory/equipment here — items need their own entities.
        */
    }

    private static void RestoreOptional(World world, Entity entity, string json)
    {
        /*
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("gender", out var gender))
            world.Add(entity, new Gender(gender.GetString()!));

        // Restore RespawnState, CommandThrottle, etc.
        */
    }
}
