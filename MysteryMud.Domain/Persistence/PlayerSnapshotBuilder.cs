using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core.Persistence;
using MysteryMud.Core.Persistence.Snapshots;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;
using System.Text.Json;

namespace MysteryMud.Domain.Persistence;

public sealed class PlayerSnapshotBuilder : ISnapshotBuilder
{
    public PlayerSnapshot Build(World world, Entity entity, long currentTick)
    {
        ref var name = ref entity.Get<Name>();
        ref var level = ref entity.Get<Level>();
        ref var location = ref entity.Get<Location>();
        ref var position = ref entity.Get<Position>();
        ref var form = ref entity.Get<Form>();
        ref var progression = ref entity.Get<Progression>();
        ref var autoBehavior = ref entity.Get<AutoBehaviour>();
        ref var baseStats = ref entity.Get<BaseStats>();
        ref var inventory = ref entity.Get<Inventory>();
        ref var equipment = ref entity.Get<Equipment>();

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
        var equippedSlots = new Dictionary<int, string>();
        var itemSnapshots = inventory.Items
            .Where(x => !x.Has<DestroyedTag>())
            .Select(itemEntity => BuildItemSnapshot(world, itemEntity, equippedSlots, currentTick))
            .ToArray();

        return new PlayerSnapshot(
            Id: world.Has<PlayerDbId>(entity) ? world.Get<PlayerDbId>(entity).Value : 0,
            Name: name.Value,
            Level: level.Value,
            LocationKey: location.Room.Id.ToString(),
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

    private static StatSnapshot[] BuildStats(World world, Entity entity)
    {
        var baseStats = entity.Get<BaseStats>();
        var effectiveStats = entity.Get<EffectiveStats>();
        var snapshots = new StatSnapshot[(int)CharacterStatKind.Count];
        for (int i = 0; i < snapshots.Length; i++)
        {
            var stat = (CharacterStatKind)i;
            snapshots[i] = new StatSnapshot(stat.ToString(), baseStats.Values[i], effectiveStats.Values[i]);
        }
        return snapshots;
    }

    private static EffectSnapshot[] BuildCharacterEffects(World world, Entity entity)
    {
        ref var effects = ref entity.Get<CharacterEffects>();
        return BuildEffects(world, effects.Data);
    }

    private static EffectSnapshot[] BuildItemEffects(World world, Entity entity)
    {
        ref var effects = ref entity.Get<ItemEffects>();
        return BuildEffects(world, effects.Data);
    }

    private static EffectSnapshot[] BuildEffects(World world, EffectsCollection effects)
    {
        // TODO
        return [];
    }

    private static AbilitySnapshot[] BuildAbilities(World world, Entity entity)
    {
        ref var learnedAbilities = ref entity.TryGetRef<LearnedAbilities>(out var hasLearnedAbilities);
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

    private static ResourceSnapshot[] BuildResources(World world, Entity entity)
    {
        ref var health = ref entity.Get<Health>();
        ref var baseHealth = ref entity.Get<BaseHealth>();
        ref var healthRegen = ref entity.Get<HealthRegen>();

        var list = new List<ResourceSnapshot>
        {
            new("Health", health.Current, baseHealth.Max, healthRegen.CurrentAmountPerSecond, healthRegen.BaseAmountPerSecond)
        };

        // Conditionally add Mana, Energy, Rage if components are present
        // if (world.Has<Mana>(entity)) { ... list.Add(new("Mana", ...)); }

        return list.ToArray();
    }

    // TODO: save items in container
    private static ItemSnapshot BuildItemSnapshot(
        World world,
        Entity item,
        Dictionary<int, string> equippedSlots,
        long currentTick)
    {
        //TODO: var vnum = item.Get<ItemTemplate>(item).Vnum;
        var vnum = 0;
        var slot = equippedSlots.GetValueOrDefault(item.Id);
        var dbId = item.Has<ItemDbId>() ? item.Get<ItemDbId>().Value : 0L;
        //var paramsJson = item.Has<ItemParams>() ? item.Get<ItemParams>().Json : null;
        var paramsJson = (string)null; // TODO

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

    private static string? BuildOptionalJson(World world, Entity entity)
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
