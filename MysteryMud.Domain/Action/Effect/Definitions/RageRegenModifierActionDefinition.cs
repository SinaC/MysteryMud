using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Character | EffectTargetKind.Item)]
public class RageRegenModifierActionDefinition : EffectActionDefinition
{
    public required ModifierKind Modifier { get; init; }
    public required EffectCompiledFormula ValueCompiledFormula { get; init; }
}
