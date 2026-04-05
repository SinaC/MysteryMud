namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class InstantHealActionDefinition : EffectActionDefinition
{
    public required Func<EffectContext, int> AmountFunc { get; init; }
}
