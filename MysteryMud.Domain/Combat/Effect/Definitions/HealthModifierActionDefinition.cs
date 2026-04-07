using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class HealthModifierActionDefinition : EffectActionDefinition
{
    public required ModifierKind Modifier { get; init; }
    public required Func<EffectContext, decimal> ValueFunc { get; init; }
}
