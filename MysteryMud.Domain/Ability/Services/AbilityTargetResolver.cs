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
using MysteryMud.GameData.Definitions;
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

        var playerSuppliedTarget = targetKind == TargetKind.Self || !string.IsNullOrWhiteSpace(targetName);

        if (playerSuppliedTarget)
        {
            // TargetKind.Self is an explicit "self" keyword from the player
            if (targetKind == TargetKind.Self)
                return TargetResolutionResult.Success([source]);

            // Try each context in order — first match wins
            foreach (var ctx in targeting.Contexts)
            {
                // TODO: don't generate a list of entities then search among the list a matching target -> apply name filter while iterating
                var candidates = GetScopedEntities(world, source, ctx.Scope)
                    .Where(x => PassesFilter(in x, ctx.Filter)).ToList();
                var candidate = EntityFinder.SelectSingleTarget(source, targetKind, targetIndex, targetName, candidates);

                if (candidate.HasValue
                    && PassesFilter(candidate.Value, ctx.Filter))
                        return TargetResolutionResult.Success([candidate.Value]);
            }
            return TargetResolutionResult.Failure(TargetResolutionStatus.TargetNotFound, "You don't see that here.");
        }

        // Optional fallback — try current opponent across character contexts only
        // No target argument supplied
        if (targeting.Requirement == AbilityTargetRequirement.Optional)
        {
            // Current opponent
            var opponent = GetCurrentOpponent(source);

            // Self fallback if any context accepts characters
            if (opponent.HasValue && targeting.Contexts.Any(c => c.Filter.HasFlag(AbilityTargetFilter.Character)))
                return TargetResolutionResult.Success([opponent.Value]);
        }

        return TargetResolutionResult.Failure(TargetResolutionStatus.NoTarget, "You must specify a target.");
    }

    // ---- AoE -------------------------------------------------------------

    private TargetResolutionResult ResolveAoE(
        in Entity source,
        AbilityTargetingDefinition targeting,
        World world)
    {
        // Union all contexts, deduplicate in case scopes overlap
        var seen = new HashSet<Entity>();
        var results = new List<Entity>();

        foreach (var ctx in targeting.Contexts)
        {
            // TODO: don't generate a list of entities then search among the list a matching target -> apply filters while iterating
            var candidates = GetScopedEntities(world, source, ctx.Scope);

            foreach (var entity in candidates)
            {
                if (entity == source) continue;
                if (!seen.Add(entity)) continue; // already included from another context
                if (!PassesFilter(entity, ctx.Filter)) continue;
                results.Add(entity);
            }
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
