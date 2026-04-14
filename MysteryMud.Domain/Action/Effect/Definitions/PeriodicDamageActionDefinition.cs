using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class PeriodicDamageActionDefinition : EffectActionDefinition
{
    public EffectFormulaEvaluationMode Mode { get; init; }
    public required EffectCompiledFormula AmountCompiledFormula { get; init; }
    public required DamageKind Kind { get; init; }
}
