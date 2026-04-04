using Arch.Core;
using MysteryMud.Domain.Effect;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Effects;

public struct EffectInstance
{
    public Entity Source;
    public Entity Target;

    public int StackCount; // for stacking effects, otherwise 1
    public EffectDefinition Definition; // TODO: delete
    public EffectRuntime EffectRuntime;
}
