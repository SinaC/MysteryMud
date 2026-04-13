using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class HealthModifierActionDefinition : EffectActionDefinition
{
    public required ModifierKind Modifier { get; init; }
    public required CompiledFormula ValueCompiledFormula { get; init; }
}
