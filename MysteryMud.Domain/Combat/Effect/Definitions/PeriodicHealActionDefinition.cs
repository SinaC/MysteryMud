namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class PeriodicHealActionDefinition : EffectActionDefinition
{
    public required Func<EffectContext, decimal> AmountFunc { get; init; }
}
