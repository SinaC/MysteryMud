using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class CharacterStatModifierActionDefinition : EffectActionDefinition
{
    public required CharacterStatKind Stat { get; init; }
    public required ModifierKind Modifier { get; init; }
    public required EffectCompiledFormula ValueCompiledFormula { get; init; }
}
