namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class PeriodicHealData : EffectActionData
{
    public string Mode { get; init; } = default!; // Snapshotted(*)/Dynamic
    public required string HealFormula { get; init; }
}
