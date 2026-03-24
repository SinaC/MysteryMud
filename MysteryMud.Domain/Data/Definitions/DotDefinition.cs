using Arch.Core;
using MysteryMud.Domain.Data.Enums;

namespace MysteryMud.Domain.Data.Definitions;

public struct DotDefinition
{
    public Func<World, Entity, Entity, int> DamageFunc;
    public DamageType DamageType;
    public int TickRate;
}
