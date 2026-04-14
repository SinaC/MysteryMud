namespace MysteryMud.Domain.Action.Effect.Definitions;

public class InstantHealActionDefinition : EffectActionDefinition
{
    public required EffectCompiledFormula AmountCompiledFormula { get; init; }
}
