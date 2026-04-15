using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EffectActionTargetAttribute : Attribute
{
    public EffectTargetKind AllowedTargets { get; }
    public EffectActionTargetAttribute(EffectTargetKind allowed) => AllowedTargets = allowed;
}
