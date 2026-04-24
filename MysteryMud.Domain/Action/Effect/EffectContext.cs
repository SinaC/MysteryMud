using TinyECS;
using MysteryMud.Core;

namespace MysteryMud.Domain.Action.Effect;

public ref struct EffectContext
{
    public EntityId? Effect; // for non-instant effect
    public EntityId Source;
    public EntityId Target;

    public int IncomingDamage;
    public int EffectiveDamageAmount;

    public int StackCount;

    public GameState State;
    public World World;
}
