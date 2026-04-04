namespace MysteryMud.Infrastructure.Persistence.Dto.Effects;

public class PeriodicHealData : EffectActionData
{
    public required string HealFormula { get; init; }
}
