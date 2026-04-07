using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class PeriodicDamageActionDefinition : EffectActionDefinition
{
    public required Func<EffectContext, decimal> AmountFunc { get; init; }
    public required DamageKind Kind { get; init; }
}
