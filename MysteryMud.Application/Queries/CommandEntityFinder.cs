using Arch.Core;
using MysteryMud.Application.Parsing;

namespace MysteryMud.Application.Queries;

public static class CommandEntityFinder
{
    // Select entities matching the target spec
    public static List<Entity> SelectTargets(Entity actor, TargetSpec spec, List<Entity> entities)
        => Domain.Queries.EntityFinder.SelectTargets(actor, spec.Kind, spec.Index, spec.Name, entities);

    public static Entity? SelectSingleTarget(Entity actor, TargetSpec spec, List<Entity> entities)
        => Domain.Queries.EntityFinder.SelectSingleTarget(actor, spec.Kind, spec.Index, spec.Name, entities);

    public static Entity? FindContainer(Entity actor, TargetSpec containerArg)
        => Domain.Queries.EntityFinder.FindContainer(actor, containerArg.Kind, containerArg.Index, containerArg.Name);
}
