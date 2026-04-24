using MysteryMud.Domain.Components;
using TinyECS;

namespace MysteryMud.Domain.Queries.Matching;

public static class NameMatcher
{
    public static bool Matches(World world, EntityId entity, string query)
    {
        ref var name = ref world.TryGetRef<Name>(entity, out var hasName);
        if (!hasName)
            return false;
        return Matches(query, name.Value);
    }

    public static bool Matches(World world, EntityId entity, ReadOnlySpan<char> query)
    {
        ref var name = ref world.TryGetRef<Name>(entity, out var hasName);
        if (!hasName)
            return false;
        return Matches(query, name.Value);
    }

    public static bool Matches(string query, string name)
    {
        return name.StartsWith(query, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Matches(ReadOnlySpan<char> query, string name)
    {
        return name.StartsWith(query, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Matches(ReadOnlySpan<char> query, ReadOnlySpan<char> name)
    {
        return name.StartsWith(query, StringComparison.OrdinalIgnoreCase);
    }
}
