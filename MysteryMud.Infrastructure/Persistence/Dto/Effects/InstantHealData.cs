namespace MysteryMud.Infrastructure.Persistence.Dto.Effects;

public class InstantHealData : EffectActionData
{
    public required string HealFormula { get; init; }
}
