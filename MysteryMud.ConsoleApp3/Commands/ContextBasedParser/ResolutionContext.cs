using Arch.Core;

namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class ResolutionContext
{
    public World World;
    public Entity Actor;

    // dynamic scope
    public List<Entity> CurrentScope;

    // resolved arguments
    public Dictionary<string, ArgValue> Args = new();

    public ResolutionContext(World world, Entity actor, List<Entity> initialScope)
    {
        World = world;
        Actor = actor;
        CurrentScope = initialScope;
    }
}
