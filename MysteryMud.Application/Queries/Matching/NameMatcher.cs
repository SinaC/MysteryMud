using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;

namespace MysteryMud.Application.Queries.Matching;

public static class NameMatcher
{
    public static bool Matches(Entity e, string query)
    {
        if (!e.Has<Name>())
            return false;
        var name = e.Get<Name>().Value;
        return Matches(query, name);
    }

    public static bool Matches(Entity e, ReadOnlySpan<char> query)
    {
        if (!e.Has<Name>())
            return false;
        var name = e.Get<Name>().Value;
        return Matches(query, name);
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
