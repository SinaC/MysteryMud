namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

public class InstantHealData : EffectActionData
{
    public required string HealFormula { get; init; }
}
