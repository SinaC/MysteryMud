using Arch.Core;
using MysteryMud.Domain.Data.Definitions;

namespace MysteryMud.Domain.Components.Effects;

public struct EffectInstance
{
    public Entity Source;
    public Entity Target;

    public EffectTemplate Template;
    public int StackCount; // for stacking effects, otherwise 1
}
