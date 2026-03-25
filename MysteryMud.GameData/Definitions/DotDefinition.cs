using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public struct DotDefinition
{
    public Func<World, Entity, Entity, int> DamageFunc;
    public DamageType DamageType;
    public int TickRate;
}
