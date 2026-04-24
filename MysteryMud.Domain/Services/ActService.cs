using MysteryMud.Domain.Formatters;
using TinyECS;

namespace MysteryMud.Domain.Services;

public class ActService : IActService
{
    private readonly World _world;

    public ActService(World world)
    {
        _world = world;
    }

    public string FormatFor(EntityId viewer, string format, params object[] args)
        => ActFormatter.FormatActOneLine(_world, viewer, format, args);
}
