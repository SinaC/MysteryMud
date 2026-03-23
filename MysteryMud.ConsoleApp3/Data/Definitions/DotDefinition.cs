using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Data.Definitions;

public struct DotDefinition
{
    public Func<World, Entity, Entity, int> DamageFunc;
    public DamageType DamageType;
    public int TickRate;
}
