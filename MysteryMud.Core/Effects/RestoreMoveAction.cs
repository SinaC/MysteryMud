using DefaultEcs;

namespace MysteryMud.Core.Effects;

public struct RestoreMoveAction
{
    public Entity Target;
    public Entity Source;
    public decimal Amount;
}
