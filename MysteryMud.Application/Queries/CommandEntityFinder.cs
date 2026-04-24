using TinyECS;
using MysteryMud.Application.Parsing;

namespace MysteryMud.Application.Queries;

public static class CommandEntityFinder
{
    // Select entities matching the target spec
    public static List<EntityId> SelectTargets(World world, EntityId actor, TargetSpec spec, List<EntityId> entities)
        => Domain.Queries.EntityFinder.SelectTargets(world, actor, spec.Kind, spec.Index, spec.Name, entities);

    public static EntityId? SelectSingleTarget(World world, EntityId actor, TargetSpec spec, List<EntityId> entities)
        => Domain.Queries.EntityFinder.SelectSingleTarget(world, actor, spec.Kind, spec.Index, spec.Name, entities);

    public static EntityId? FindContainer(World world, EntityId actor, TargetSpec containerArg)
        => Domain.Queries.EntityFinder.FindContainer(world, actor, containerArg.Kind, containerArg.Index, containerArg.Name);
}
