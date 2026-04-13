using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class PeriodicHealActionDefinition : EffectActionDefinition
{
    public EffectFormulaEvaluationMode Mode { get; init; }
    public required CompiledFormula AmountCompiledFormula { get; init; }
}
