namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class InstantDamageData : EffectActionData
{
    public required string DamageFormula { get; init; }
    public required string DamageKind { get; init; }
};
