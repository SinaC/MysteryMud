using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Character)]
public class PeriodicHealActionDefinition : EffectActionDefinition
{
    public EffectFormulaEvaluationMode Mode { get; init; }
    public required EffectCompiledFormula AmountCompiledFormula { get; init; }
}
