namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class RegenModifierData : EffectActionData
{
    public required string Resource { get; init; }
    public required string Mode { get; init; }
    public required string ValueFormula { get; init; }
}
