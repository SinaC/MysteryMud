using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;

namespace MysteryMud.Domain.Systems;

public static class TargetingSystem
{
    // Select entities matching the target spec
    public static List<Entity> SelectTargets(
        Entity actor,
        TargetSpec spec,
        List<Entity> entities)
    {
        var results = new List<Entity>();

        if (spec.Kind == TargetKind.Self)
        {
            results.Add(actor);
            return results;
        }

        int matchCount = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!Matches(entity, spec.Name))
                continue;

            matchCount++;

            if (spec.Kind == TargetKind.Single)
            {
                results.Add(entity);
                return results;
            }
            else if (spec.Kind == TargetKind.Indexed)
            {
                if (matchCount == spec.Index)
                {
                    results.Add(entity);
                    return results;
                }
            }
            else if (spec.Kind == TargetKind.All)
            {
                results.Add(entity);
            }
        }
        return results;
    }

    public static Entity SelectSingleTarget(Entity actor, TargetSpec spec, List<Entity> entities)
    {
        if (spec.Kind == TargetKind.Self)
        {
            return actor;
        }

        int matchCount = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!Matches(entity, spec.Name))
                continue;

            matchCount++;

            if (spec.Kind == TargetKind.Single)
            {
                return entity;
            }
            else if (spec.Kind == TargetKind.Indexed)
            {
                if (matchCount == spec.Index)
                {
                    return entity;
                }
            }
            else if (spec.Kind == TargetKind.All)
            {
                return entity; // For 'All', just return the first match (or consider throwing an exception)
            }
        }
        return default;
    }

    // Simple prefix matching, case-insensitive
    public static bool Matches(Entity e, ReadOnlySpan<char> query)
    {
        if (e.Has<Dead>() || e.Has<DestroyedTag>()) // don't consider dead/destroyed entities as valid targets
            return false;
        if (query.IsEmpty) return true; // 'all' or unspecified
        return NameSystem.Matches(e, query);
    }
}
