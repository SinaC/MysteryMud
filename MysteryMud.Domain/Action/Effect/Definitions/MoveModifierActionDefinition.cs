using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Character | EffectTargetKind.Item)]
public class MoveModifierActionDefinition : EffectActionDefinition
{
    public required ModifierKind Modifier { get; init; }
    public required EffectCompiledFormula ValueCompiledFormula { get; init; }
}
