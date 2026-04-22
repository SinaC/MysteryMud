namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class InstantRestoreMoveData : EffectActionData
{
    public required string MoveFormula { get; init; }
}
