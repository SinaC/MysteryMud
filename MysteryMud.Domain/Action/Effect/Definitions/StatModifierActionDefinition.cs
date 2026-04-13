using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class StatModifierActionDefinition : EffectActionDefinition
{
    public required StatKind Stat { get; init; }
    public required ModifierKind Modifier { get; init; }
    public required CompiledFormula ValueCompiledFormula { get; init; }
}
