namespace MysteryMud.Domain.Action.Effect.Definitions;

public class PeriodicHealActionDefinition : EffectActionDefinition
{
    public required Func<EffectContext, decimal> AmountFunc { get; init; }
}
