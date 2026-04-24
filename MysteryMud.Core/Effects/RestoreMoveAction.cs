using TinyECS;

namespace MysteryMud.Core.Effects;

public struct RestoreMoveAction
{
    public EntityId Target;
    public EntityId Source;
    public decimal Amount;
}
