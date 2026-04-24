using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Resources;

public struct CostContext
{
    public EntityId Entity;
    public ResourceKind Kind;
    public int BaseAmount;
    public int FinalAmount;
}
