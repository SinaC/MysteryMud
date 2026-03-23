using Arch.Core;

namespace MysteryMud.ConsoleApp3.Data.Definitions;

public struct HotDefinition
{
    public Func<World, Entity, Entity, int> HealFunc;
    public int TickRate;
}
