using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Resources;

public struct CostContext
{
    public Entity Entity;
    public ResourceKind Kind;
    public int BaseAmount;
    public int FinalAmount;
}
