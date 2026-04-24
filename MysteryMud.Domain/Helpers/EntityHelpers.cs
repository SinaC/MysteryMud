using MysteryMud.Domain.Components;
using TinyECS;

namespace MysteryMud.Domain.Helpers;

public static class EntityHelpers
{
    public static string Name(World world, EntityId entity)
        => world.Get<Name>(entity).Value;

    public static string DisplayName(World world, EntityId entity)
        => BuildDisplayName(world, entity);

    public static string DebugName(World world, EntityId entity)
        => BuildDebugName(world, entity);

    private static string BuildDisplayName(World world, EntityId entity)
    {
        if (!world.IsAlive(entity))
            return $"DEAD [{entity.Index}]";
        ref var description = ref world.TryGetRef<Description>(entity, out var descriptionExists);
        if (descriptionExists)
            return description.Value;
        ref var name = ref world.TryGetRef<Name>(entity, out var nameExists);
        if (nameExists)
            return name.Value;
        return entity.Index.ToString();
    }

    private static string BuildDebugName(World world, EntityId entity)
    {
        if (!world.IsAlive(entity))
            return $"DEAD ({entity.Index})";
        ref var name = ref world.TryGetRef<Name>(entity, out var nameExists);
        if (nameExists)
            return $"{name.Value}[{entity.Index}]";
        return $"[{entity.Index}]";
    }
}

