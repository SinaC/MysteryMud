using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Resources;

public struct CostContext
{
    public Entity Entity;
    public ResourceKind Kind;
    public int BaseAmount;
    public int FinalAmount;
}
