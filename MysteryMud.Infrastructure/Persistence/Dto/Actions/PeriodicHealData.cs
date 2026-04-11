namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class PeriodicHealData : EffectActionData
{
    public required string HealFormula { get; init; }
}
