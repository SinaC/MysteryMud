using DefaultEcs;
using MysteryMud.Core.Persistence;
using MysteryMud.Core.Persistence.Snapshots;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
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
            Id: entity.Has<PlayerDbId>() ? entity.Get<PlayerDbId>().Value : 0,
            Name: name.Value,
            Level: level.Value,
            LocationKey: location.Room.GetHashCode().ToString(),
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
        if (!entity.Has<LearnedAbilities>())
            return [];
        ref var learnedAbilities = ref entity.Get<LearnedAbilities>();
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
        ref var move = ref entity.Get<Move>();
        ref var baseMove = ref entity.Get<BaseMove>();
        ref var moveRegen = ref entity.Get<MoveRegen>();

        var list = new List<ResourceSnapshot>
        {
            new("Health", health.Current, baseHealth.Max, healthRegen.CurrentAmountPerSecond, healthRegen.BaseAmountPerSecond),
            new("Move", move.Current, baseMove.Max, moveRegen.CurrentAmountPerSecond, moveRegen.BaseAmountPerSecond)
        };

        // Conditionally add Mana, Energy, Rage if components are present
        if (entity.Has<Mana>())
        {
            ref var mana = ref entity.Get<Mana>();
            ref var baseMana = ref entity.Get<BaseMana>();
            ref var manaRegen = ref entity.Get<ManaRegen>();
            list.Add(new ("Mana", mana.Current, baseMana.Max, manaRegen.CurrentAmountPerSecond, manaRegen.BaseAmountPerSecond));
        }
        if (entity.Has<Energy>())
        {
            ref var energy = ref entity.Get<Energy>();
            ref var baseEnergy = ref entity.Get<BaseEnergy>();
            ref var energyRegen = ref entity.Get<EnergyRegen>();
            list.Add(new("Energy", energy.Current, baseEnergy.Max, energyRegen.CurrentAmountPerSecond, energyRegen.BaseAmountPerSecond));
        }
        if (entity.Has<Rage>())
        {
            ref var rage = ref entity.Get<Rage>();
            ref var baseRage = ref entity.Get<BaseRage>();
            ref var rageDecay = ref entity.Get<RageDecay>();
            list.Add(new("Rage", rage.Current, baseRage.Max, rageDecay.CurrentAmountPerSecond, rageDecay.BaseAmountPerSecond));
        }

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
        var slot = equippedSlots.GetValueOrDefault(item.GetHashCode());
        var dbId = item.Has<ItemDbId>() ? item.Get<ItemDbId>().Value : 0L;
        //var paramsJson = item.Has<ItemParams>() ? item.Get<ItemParams>().Json : null;
        var paramsJson = (string)null!; // TODO

        long? containerId = null;
        if (item.Has<ContainedIn>())
        {
            var container = item.Get<ContainedIn>().Container;
            if (container.Has<ItemDbId>())
                containerId = container.Get<ItemDbId>().Value;
        }

        var effects = BuildItemEffects(world, item);

        return new ItemSnapshot(dbId, vnum, slot, containerId, paramsJson, effects);
    }

    private static string? BuildOptionalJson(World world, Entity entity)
    {
        var parts = new Dictionary<string, object?>();

        if (entity.Has<Gender>())
            parts["gender"] = entity.Get<Gender>().Value;

        // Add RespawnState, CommandThrottle, etc. here

        return parts.Count > 0
            ? JsonSerializer.Serialize(parts)
            : null;
    }
}
