using MysteryMud.Core.Persistence;
using MysteryMud.Core.Persistence.Snapshots;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;
using System.Text.Json;
using TinyECS;

namespace MysteryMud.Domain.Persistence;

public sealed class PlayerSnapshotBuilder : ISnapshotBuilder
{
    public PlayerSnapshot Build(World world, EntityId entity, long currentTick)
    {
        ref var name = ref world.Get<Name>(entity);
        ref var level = ref world.Get<Level>(entity);
        ref var location = ref world.Get<Location>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var form = ref world.Get<Form>(entity);
        ref var progression = ref world.Get<Progression>(entity);
        ref var autoBehavior = ref world.Get<AutoBehaviour>(entity);
        ref var baseStats = ref world.Get<BaseStats>(entity);
        ref var inventory = ref world.Get<Inventory>(entity);
        ref var equipment = ref world.Get<Equipment>(entity);

        // Optional components
        string? optionalJson = BuildOptionalJson(world, entity);

        // Stats
        var stats = BuildStats(world, entity);

        // Resources — build from Health (+ Mana/Energy/Rage if present)
        var resources = BuildResources(world, entity);

        // Effects
        var effectSnapshots = BuildCharacterEffects(world, entity);

        // Abilities
        var abilitySnapshots = BuildAbilities(world, entity);

        // Items — merge inventory + equipped, get db id from ItemDbId component if present
        //var equippedSlots = equipment.Slots.ToDictionary(s => s.Item.Id, s => s.Slot);
        var equippedSlots = new Dictionary<uint, string>();
        var itemSnapshots = inventory.Items
            .Where(x => !world.Has<DestroyedTag>(x))
            .Select(itemEntity => BuildItemSnapshot(world, itemEntity, equippedSlots, currentTick))
            .ToArray();

        return new PlayerSnapshot(
            Id: world.Has<PlayerDbId>(entity) ? world.Get<PlayerDbId>(entity).Value : 0,
            Name: name.Value,
            Level: level.Value,
            LocationKey: location.Room.Index.ToString(),
            Position: position.Value.ToString(),
            Form: form.Value.ToString(),
            TotalXp: progression.Experience,
            AutoBehavior: (int)autoBehavior.Flags,
            OptionalJson: optionalJson,
            Stats: stats,
            Resources: resources,
            Effects: effectSnapshots,
            Abilities: abilitySnapshots,
            Items: itemSnapshots);
    }

    private static StatSnapshot[] BuildStats(World world, EntityId entity)
    {
        ref var baseStats = ref world.Get<BaseStats>(entity);
        ref var effectiveStats = ref world.Get<EffectiveStats>(entity);
        var snapshots = new StatSnapshot[(int)CharacterStatKind.Count];
        for (int i = 0; i < snapshots.Length; i++)
        {
            var stat = (CharacterStatKind)i;
            snapshots[i] = new StatSnapshot(stat.ToString(), baseStats.Values[i], effectiveStats.Values[i]);
        }
        return snapshots;
    }

    private static EffectSnapshot[] BuildCharacterEffects(World world, EntityId entity)
    {
        ref var effects = ref world.Get<CharacterEffects>(entity);
        return BuildEffects(world, effects.Data);
    }

    private static EffectSnapshot[] BuildItemEffects(World world, EntityId entity)
    {
        ref var effects = ref world.Get<ItemEffects>(entity);
        return BuildEffects(world, effects.Data);
    }

    private static EffectSnapshot[] BuildEffects(World world, EffectsCollection effects)
    {
        // TODO
        return [];
    }

    private static AbilitySnapshot[] BuildAbilities(World world, EntityId entity)
    {
        ref var learnedAbilities = ref world.TryGetRef<LearnedAbilities>(entity, out var hasLearnedAbilities);
        if (!hasLearnedAbilities)
            return [];
        // TODO
        return [];
        //var snapshots = new AbilitySnapshot[learnedAbilities.Entries.Count];
        //for (int i = 0; i < snapshots.Length; i++)
        //{
        //    var learnedAbility = learnedAbilities.Entries[i];
        //    var abilityName = learnedAbility.AbilityId.ToString(); // TODO: get ability name
        //    var className = learnedAbility.ClassId.ToString(); // TODO: get class name
        //    // TODO: get cooldown
        //    // TODO: get charges
        //    snapshots[i] = new AbilitySnapshot(abilityName, className, learnedAbility.LearnedPercent, learnedAbility.LearnedLevel, learnedAbility.MasterTier, null, null);
        //}
        //return snapshots;
    }

    private static ResourceSnapshot[] BuildResources(World world, EntityId entity)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var baseHealth = ref world.Get<BaseHealth>(entity);
        ref var healthRegen = ref world.Get<HealthRegen>(entity);
        ref var move = ref world.Get<Move>(entity);
        ref var baseMove = ref world.Get<BaseMove>(entity);
        ref var moveRegen = ref world.Get<MoveRegen>(entity);

        var list = new List<ResourceSnapshot>
        {
            new("Health", health.Current, baseHealth.Max, healthRegen.CurrentAmountPerSecond, healthRegen.BaseAmountPerSecond),
            new("Move", move.Current, baseMove.Max, moveRegen.CurrentAmountPerSecond, moveRegen.BaseAmountPerSecond)
        };

        // Conditionally add Mana, Energy, Rage if components are present
        // if (world.Has<Mana>(entity)) { ... list.Add(new("Mana", ...)); }

        return list.ToArray();
    }

    // TODO: save items in container
    private static ItemSnapshot BuildItemSnapshot(
        World world,
        EntityId item,
        Dictionary<uint, string> equippedSlots,
        long currentTick)
    {
        //TODO: var vnum = item.Get<ItemTemplate>(item).Vnum;
        var vnum = 0;
        var slot = equippedSlots.GetValueOrDefault(item.Index);
        var dbId = world.Has<ItemDbId>(item) ? world.Get<ItemDbId>(item).Value : 0L;
        //var paramsJson = item.Has<ItemParams>() ? item.Get<ItemParams>().Json : null;
        var paramsJson = (string)null!; // TODO

        long? containerId = null;
        if (world.Has<ContainedIn>(item))
        {
            var container = world.Get<ContainedIn>(item).Container;
            if (world.Has<ItemDbId>(container))
                containerId = world.Get<ItemDbId>(container).Value;
        }

        var effects = BuildItemEffects(world, item);

        return new ItemSnapshot(dbId, vnum, slot, containerId, paramsJson, effects);
    }

    private static string? BuildOptionalJson(World world, EntityId entity)
    {
        var parts = new Dictionary<string, object?>();

        if (world.Has<Gender>(entity))
            parts["gender"] = world.Get<Gender>(entity).Value;

        // Add RespawnState, CommandThrottle, etc. here

        return parts.Count > 0
            ? JsonSerializer.Serialize(parts)
            : null;
    }
}
