namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class InstantHealActionDefinition : EffectActionDefinition
{
    public required Func<EffectContext, decimal> AmountFunc { get; init; }
}
