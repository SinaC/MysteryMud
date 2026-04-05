using Arch.Core;
using MysteryMud.Domain.Combat.Effect;

namespace MysteryMud.Domain.Components.Effects;

public struct EffectInstance
{
    public Entity Source;
    public Entity Target;

    public int StackCount; // for stacking effects, otherwise 1
    public EffectRuntime EffectRuntime;
}
