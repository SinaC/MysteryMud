namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class PeriodicDamageData : EffectActionData
{
    // (*): default
    public string Mode { get; init; } = default!; // Snapshotted(*)/Dynamic
    public required string DamageFormula { get; init; }
    public required string DamageKind { get; init; }
}
