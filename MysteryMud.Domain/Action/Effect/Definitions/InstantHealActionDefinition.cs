namespace MysteryMud.Domain.Action.Effect.Definitions;

public class InstantHealActionDefinition : EffectActionDefinition
{
    public required Func<EffectContext, decimal> AmountFunc { get; init; }
}
