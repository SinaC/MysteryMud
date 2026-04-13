using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class PeriodicDamageActionDefinition : EffectActionDefinition
{
    public EffectFormulaEvaluationMode Mode { get; init; }
    public required CompiledFormula AmountCompiledFormula { get; init; }
    public required DamageKind Kind { get; init; }
}
