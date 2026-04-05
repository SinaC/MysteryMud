namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

public class PeriodicHealData : EffectActionData
{
    public required string HealFormula { get; init; }
}
