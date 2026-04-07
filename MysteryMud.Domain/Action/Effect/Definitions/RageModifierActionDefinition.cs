using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class RageModifierActionDefinition : EffectActionDefinition
{
    public required ModifierKind Modifier { get; init; }
    public required Func<EffectContext, decimal> ValueFunc { get; init; }
}
