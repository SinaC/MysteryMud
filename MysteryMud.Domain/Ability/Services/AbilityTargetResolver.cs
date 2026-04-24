using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Services;

public sealed class AbilityTargetResolver : IAbilityTargetResolver
{
    private readonly World _world;

    public AbilityTargetResolver(World world)
    {
        _world = world;
    }

    public TargetResolutionResult Resolve(
        in EntityId source,
        TargetKind targetKind,
        int targetIndex,
        string targetName,
        AbilityTargetingDefinition targeting)
    {
        return targeting.Selection switch
        {
            AbilityTargetSelection.Single => ResolveSingle(source, targetKind, targetIndex, targetName, targeting),
            AbilityTargetSelection.AoE => ResolveAoE(source, targeting),
            _ => TargetResolutionResult.Failure(TargetResolutionStatus.NoTarget)
        };
    }

    public TargetResolutionResult Resolve(
            in EntityId source,
            AbilityTargetingDefinition targeting)
        => Resolve(in source, TargetKind.Self, 0, null!, targeting);

    // ---- Single ----------------------------------------------------------

    private TargetResolutionResult ResolveSingle(
        in EntityId source,
        TargetKind targetKind,
        int targetIndex,
        string targetName,
        AbilityTargetingDefinition targeting)
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
                var candidates = GetScopedEntities(source, ctx.Scope)
                    .Where(x => PassesFilter(in x, ctx.Filter)).ToList();
                var candidate = EntityFinder.SelectSingleTarget(_world, source, targetKind, targetIndex, targetName, candidates);

                if (candidate.HasValue
                    && PassesFilter(candidate.Value, ctx.Filter))
                        return TargetResolutionResult.Success([candidate.Value]);
            }
            return TargetResolutionResult.Failure(TargetResolutionStatus.TargetNotFound, "You don't see that here.");
        }

        // Optional Opponent fallback — try current opponent across character contexts only
        // No target argument supplied
        if (targeting.Requirement == AbilityTargetRequirement.OptionalOpponent)
        {
            // Current opponent
            var opponent = GetCurrentOpponent(source);

            // Opponent fallback if any context accepts characters
            if (opponent.HasValue && targeting.Contexts.Any(c => c.Filter.HasFlag(AbilityTargetFilter.Character)))
                return TargetResolutionResult.Success([opponent.Value]);
        }

        // Optional Opponent fallback — try current opponent across character contexts only
        // No target argument supplied
        if (targeting.Requirement == AbilityTargetRequirement.OptionalSelf)
        {
            // Self
            var self = source;

            return TargetResolutionResult.Success([self]);
        }


        return TargetResolutionResult.Failure(TargetResolutionStatus.NoTarget, "You must specify a target.");
    }

    // ---- AoE -------------------------------------------------------------

    private TargetResolutionResult ResolveAoE(
        in EntityId source,
        AbilityTargetingDefinition targeting)
    {
        // Union all contexts, deduplicate in case scopes overlap
        var seen = new HashSet<EntityId>();
        var results = new List<EntityId>();

        foreach (var ctx in targeting.Contexts)
        {
            // TODO: don't generate a list of entities then search among the list a matching target -> apply filters while iterating
            var candidates = GetScopedEntities(source, ctx.Scope);

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

    private IEnumerable<EntityId> GetScopedEntities(in EntityId source, AbilityTargetScope scope)
        => scope switch
        {
            AbilityTargetScope.Self => [source],
            AbilityTargetScope.Room => GetEntitiesInSameRoom(source),
            AbilityTargetScope.World => GetAllEntities(),
            AbilityTargetScope.Inventory => GetInventoryEntities(source),
            _ => []
        };

    private IEnumerable<EntityId> GetEntitiesInSameRoom(EntityId source)
    {
        ref var room = ref _world.Get<Location>(source).Room;
        ref var roomContents = ref _world.Get<RoomContents>(room);
        return [.. roomContents.Characters, .. roomContents.Items];
    }

    private static IEnumerable<EntityId> GetAllEntities()
    {
        // TODO
        return [];
    }

    private IEnumerable<EntityId> GetInventoryEntities(EntityId source)
    {
        ref var inventory = ref _world.TryGetRef<Inventory>(source, out var hasInventory);
        if (!hasInventory)
            return [];
        return inventory.Items;
    }

    private EntityId? GetCurrentOpponent(EntityId source)
    {
        ref var combatState = ref _world.TryGetRef<CombatState>(source, out var inCombat);
        if (!inCombat)
            return null;
        return combatState.Target;
    }

    private bool PassesFilter(in EntityId entity, AbilityTargetFilter filter)
    {
        // Delegate to world component checks — placeholder pattern shown here.
        // Replace with actual Arch component lookups.
        if (filter.HasFlag(AbilityTargetFilter.Player) && IsPlayer(entity)) return true;
        if (filter.HasFlag(AbilityTargetFilter.NPC) && IsNPC(entity)) return true;
        if (filter.HasFlag(AbilityTargetFilter.Item) && IsItem(entity)) return true;
        return false;
    }

    // Thin wrappers — replace with actual ECS Has<T> calls
    private bool IsPlayer(in EntityId entity) => _world.Has<PlayerTag>(entity);
    private bool IsNPC(in EntityId entity) => _world.Has<NpcTag>(entity);
    private bool IsItem(in EntityId entity) => _world.Has<ItemTag>(entity);
}
