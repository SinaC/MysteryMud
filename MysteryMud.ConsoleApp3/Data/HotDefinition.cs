using Arch.Core;

namespace MysteryMud.ConsoleApp3.Data;

public struct HotDefinition
{
    public Func<World, Entity, Entity, int> HealFunc;
    public int TickRate;
}
