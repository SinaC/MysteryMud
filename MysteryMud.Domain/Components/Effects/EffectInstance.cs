using TinyECS;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Components.Effects;

public struct EffectInstance
{
    public EntityId Source;
    public EntityId Target;

    public int StackCount; // for stacking effects, otherwise 1
    public EffectRuntime EffectRuntime;
}
