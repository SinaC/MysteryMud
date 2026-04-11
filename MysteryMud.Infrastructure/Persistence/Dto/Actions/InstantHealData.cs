namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class InstantHealData : EffectActionData
{
    public required string HealFormula { get; init; }
}
