using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries.Matching;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Queries;

public static class EntityFinder
{
    public static List<EntityId> SelectTargets(World world, EntityId actor, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName, List<EntityId> entities)
    {
        var results = new List<EntityId>();

        if (targetKind == TargetKind.Self)
        {
            results.Add(actor);
            return results;
        }

        int matchCount = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!Matches(world, entity, targetName, targetKind == TargetKind.All)) // of course, 'All' with an empty name should match everything
                continue;

            matchCount++;

            if (targetKind == TargetKind.Single)
            {
                results.Add(entity);
                return results;
            }
            else if (targetKind == TargetKind.Indexed)
            {
                if (matchCount == targetIndex)
                {
                    results.Add(entity);
                    return results;
                }
            }
            else if (targetKind == TargetKind.All)
            {
                results.Add(entity);
            }
        }
        return results;
    }

    public static EntityId? SelectSingleTarget(World world, EntityId actor, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName, List<EntityId> entities)
    {
        if (targetKind == TargetKind.Self)
        {
            return actor;
        }

        int matchCount = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!Matches(world, entity, targetName, targetKind == TargetKind.All)) // of course, 'All' with an empty name should match everything
                continue;

            matchCount++;

            if (targetKind == TargetKind.Single)
            {
                return entity;
            }
            else if (targetKind == TargetKind.Indexed)
            {
                if (matchCount == targetIndex)
                {
                    return entity;
                }
            }
            else if (targetKind == TargetKind.All)
            {
                return entity; // For 'All', just return the first match (or consider throwing an exception)
            }
        }
        return null;
    }

    public static EntityId? FindContainer(World world, EntityId actor, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName)
    {
        // Search in room first
        var room = world.Get<Location>(actor).Room;
        var roomContents = world.Get<RoomContents>(room);

        var container = SelectSingleTarget(world, actor, targetKind, targetIndex, targetName, roomContents.Items);
        if (container != default)
            return container;

        // Then inventory
        var inventory = world.Get<Inventory>(actor);
        container = SelectSingleTarget(world, actor, targetKind, targetIndex, targetName, inventory.Items);
        if (container != default)
            return container;

        return null;
    }

    // Simple prefix matching, case-insensitive
    public static bool Matches(World world, EntityId entity, ReadOnlySpan<char> query, bool isAll = false)
    {
        if (world.Has<Dead>(entity) || world.Has<DestroyedTag>(entity)) // don't consider dead/destroyed entities as valid targets
            return false;
        if (query.IsEmpty)
            return isAll;
        return NameMatcher.Matches(world, entity, query);
    }
}
