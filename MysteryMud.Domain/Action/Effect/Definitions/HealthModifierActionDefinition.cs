using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class HealthModifierActionDefinition : EffectActionDefinition
{
    public required ModifierKind Modifier { get; init; }
    public required Func<EffectContext, decimal> ValueFunc { get; init; }
}
