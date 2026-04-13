using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class PeriodicHealActionDefinition : EffectActionDefinition
{
    public EffectFormulaEvaluationMode Mode { get; init; }
    public required Func<EffectContext, decimal> AmountFunc { get; init; }
}
