using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Resources;

public struct CannotPayCostsResult
{
    public CannotPayCostsReason Reason;
    public ResourceKind Kind;
}

public enum CannotPayCostsReason
{
    NotEnoughResource,
    ResourceNotAvailable,
}
