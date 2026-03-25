using Arch.Core;

namespace MysteryMud.GameData.Definitions;

public struct HotDefinition
{
    public Func<World, Entity, Entity, int> HealFunc;
    public int TickRate;
}
