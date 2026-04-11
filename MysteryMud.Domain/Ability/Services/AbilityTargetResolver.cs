using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Services;

public sealed class AbilityTargetResolver : IAbilityTargetResolver
{
    public TargetResolutionResult Resolve(
        in Entity source,
        TargetKind targetKind,
        int targetIndex,
        string targetName,
        AbilityTargetingDefinition targeting,
        GameState state)
    {
        return targeting.Selection switch
        {
            AbilityTargetSelection.Single => ResolveSingle(source, targetKind, targetIndex, targetName, targeting, state.World),
            AbilityTargetSelection.AoE => ResolveAoE(source, targeting, state.World),
            _ => TargetResolutionResult.Failure(TargetResolutionStatus.NoTarget)
        };
    }

    public TargetResolutionResult Resolve(
            in Entity source,
            AbilityTargetingDefinition targeting,
            GameState state)
        => Resolve(in source, TargetKind.Self, 0, null!, targeting, state);

    // ---- Single ----------------------------------------------------------

    private TargetResolutionResult ResolveSingle(
        in Entity source,
        TargetKind targetKind,
        int targetIndex,
        string targetName,
        AbilityTargetingDefinition targeting,
        World world)
    {
        // Requirement.None → always self
        if (targeting.Requirement == AbilityTargetRequirement.None)
            return TargetResolutionResult.Success([source]);

        Entity? candidate = null;
        bool playerSuppliedTarget = !string.IsNullOrWhiteSpace(targetName);

        if (playerSuppliedTarget)
        {
            // TargetKind.Self is an explicit "self" keyword from the player
            if (targetKind == TargetKind.Self)
            {
                candidate = source;
            }
            else
            {
                // TODO: don't generate a list of entities then search among the list a matching target -> apply name filter while iterating
                var candidates = GetScopedEntities(world, source, targeting.Scope)
                    .Where(x => PassesFilter(in x, targeting.Filter)).ToList();
                candidate = EntityFinder.SelectSingleTarget(source, targetKind, targetIndex, targetName, candidates);

                if (!candidate.HasValue)
                    return TargetResolutionResult.Failure(
                        TargetResolutionStatus.TargetNotFound,
                        "You don't see that here.");

                if (!PassesFilter(candidate.Value, targeting.Filter))
                    return TargetResolutionResult.Failure(
                        TargetResolutionStatus.InvalidTarget,
                        "That is not a valid target for this ability.");
            }
        }
        else
        {
            // No target argument supplied
            if (targeting.Requirement == AbilityTargetRequirement.Optional)
            {
                // Fallback 1: current opponent
                candidate = GetCurrentOpponent(source);

                // Fallback 2: self, if the filter accepts characters
                if (!candidate.HasValue && targeting.Filter.HasFlag(AbilityTargetFilter.Character))
                    candidate = source;
            }
            // Requirement.Mandatory with no argument → fail
        }

        if (!candidate.HasValue)
            return TargetResolutionResult.Failure(
                TargetResolutionStatus.NoTarget,
                "You must specify a target.");

        return TargetResolutionResult.Success([candidate.Value]);
    }

    // ---- AoE -------------------------------------------------------------

    private TargetResolutionResult ResolveAoE(
        in Entity source,
        AbilityTargetingDefinition targeting,
        World world)
    {
        // TODO: don't generate a list of entities then search among the list a matching target -> apply filters while iterating
        var candidates = GetScopedEntities(world, source, targeting.Scope);
        var results = new List<Entity>();

        foreach (var entity in candidates)
        {
            if (entity == source) continue;
            if (!PassesFilter(entity, targeting.Filter)) continue;
            results.Add(entity);
        }

        // AoE always succeeds even with 0 targets
        return TargetResolutionResult.Success(results);
    }

    // ---- Helpers ---------------------------------------------------------

    private static IEnumerable<Entity> GetScopedEntities(World world, in Entity source, AbilityTargetScope scope)
        => scope switch
        {
            AbilityTargetScope.Self => [source],
            AbilityTargetScope.Room => GetEntitiesInSameRoom(source),
            AbilityTargetScope.World => GetAllEntities(world),
            AbilityTargetScope.Inventory => GetInventoryEntities(source),
            _ => []
        };

    private static IEnumerable<Entity> GetEntitiesInSameRoom(Entity source)
    {
        var roomContents = source.Get<Location>().Room.Get<RoomContents>();
        return [.. roomContents.Characters, .. roomContents.Items];
    }

    private static IEnumerable<Entity> GetAllEntities(World world)
    {
        // TODO
        return [];
    }

    private static IEnumerable<Entity> GetInventoryEntities(Entity source)
    {
        ref var inventory = ref source.TryGetRef<Inventory>(out var hasInventory);
        if (!hasInventory)
            return [];
        return inventory.Items;
    }

    private static Entity? GetCurrentOpponent(Entity source)
    {
        ref var combatState = ref source.TryGetRef<CombatState>(out var inCombat);
        if (!inCombat)
            return null;
        return combatState.Target;
    }

    private static bool PassesFilter(in Entity entity, AbilityTargetFilter filter)
    {
        // Delegate to world component checks — placeholder pattern shown here.
        // Replace with actual Arch component lookups.
        if (filter.HasFlag(AbilityTargetFilter.Player) && IsPlayer(entity)) return true;
        if (filter.HasFlag(AbilityTargetFilter.NPC) && IsNPC(entity)) return true;
        if (filter.HasFlag(AbilityTargetFilter.Item) && IsItem(entity)) return true;
        return false;
    }

    // Thin wrappers — replace with actual Arch Has<T> calls
    private static bool IsPlayer(in Entity e) => e.Has<PlayerTag>();
    private static bool IsNPC(in Entity e) => e.Has<NpcTag>();
    private static bool IsItem(in Entity e) => e.Has<ItemTag>();
}
