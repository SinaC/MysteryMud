using MysteryMud.Domain.Services;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class InstantHealActionDefinition : EffectActionDefinition
{
    public required CompiledFormula AmountCompiledFormula { get; init; }
}
