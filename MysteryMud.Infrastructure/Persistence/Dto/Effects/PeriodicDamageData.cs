namespace MysteryMud.Infrastructure.Persistence.Dto.Effects;

public class PeriodicDamageData : EffectActionData
{
    public required string DamageFormula { get; init; }
    public required string DamageKind { get; init; }
}
