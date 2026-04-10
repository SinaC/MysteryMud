using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries.Matching;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Queries;

public static class EntityFinder
{
    public static List<Entity> SelectTargets(Entity actor, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName, List<Entity> entities)
    {
        var results = new List<Entity>();

        if (targetKind == TargetKind.Self)
        {
            results.Add(actor);
            return results;
        }

        int matchCount = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!Matches(entity, targetName, targetKind == TargetKind.All)) // of course, 'All' with an empty name should match everything
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

    public static Entity SelectSingleTarget(Entity actor, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName, List<Entity> entities)
    {
        if (targetKind == TargetKind.Self)
        {
            return actor;
        }

        int matchCount = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!Matches(entity, targetName, targetKind == TargetKind.All)) // of course, 'All' with an empty name should match everything
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
        return default;
    }

    public static Entity FindContainer(Entity actor, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName)
    {
        // Search in room first
        var room = actor.Get<Location>().Room;
        var roomContents = room.Get<RoomContents>();

        var container = SelectSingleTarget(actor, targetKind, targetIndex, targetName, roomContents.Items);
        if (container != default)
            return container;

        // Then inventory
        var inventory = actor.Get<Inventory>();
        container = SelectSingleTarget(actor, targetKind, targetIndex, targetName, inventory.Items);
        if (container != default)
            return container;

        return default;
    }

    // Simple prefix matching, case-insensitive
    public static bool Matches(Entity e, ReadOnlySpan<char> query, bool isAll = false)
    {
        if (e.Has<Dead>() || e.Has<DestroyedTag>()) // don't consider dead/destroyed entities as valid targets
            return false;
        if (query.IsEmpty)
            return isAll;
        return NameMatcher.Matches(e, query);
    }
}
