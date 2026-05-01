namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class GenerateThreatData : EffectActionData
{
    public required string AmountFormula { get; init; }
}
