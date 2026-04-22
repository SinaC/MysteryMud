namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class InstantRestoreResourceData : EffectActionData
{
    public required string Resource { get; init; }
    public required string ValueFormula { get; init; }
}
