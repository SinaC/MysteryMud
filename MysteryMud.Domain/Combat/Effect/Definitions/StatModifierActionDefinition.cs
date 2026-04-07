using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class StatModifierActionDefinition : EffectActionDefinition
{
    public required StatKind Stat { get; init; }
    public required ModifierKind Modifier { get; init; }
    public required Func<EffectContext, decimal> ValueFunc { get; init; }
}
