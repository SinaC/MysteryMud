using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Definitions;

namespace MysteryMud.ConsoleApp3.Domain.Components.Effects;

struct EffectInstance
{
    public Entity Source;
    public Entity Target;

    public EffectTemplate Template;
    public int StackCount; // for stacking effects, otherwise 1
}
