using DefaultEcs;
using MysteryMud.Core;

namespace MysteryMud.Domain.Action.Effect;

public ref struct EffectContext
{
    public Entity? Effect; // for non-instant effect
    public Entity Source;
    public Entity Target;

    public int IncomingDamage;
    public int EffectiveDamageAmount;

    public int StackCount;

    public GameState State;
}
