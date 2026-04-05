namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class PeriodicHealActionDefinition : EffectActionDefinition
{
    public required Func<EffectContext, int> AmountFunc { get; init; }
}
