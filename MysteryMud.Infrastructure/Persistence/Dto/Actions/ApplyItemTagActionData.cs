namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class ApplyItemTagActionData : EffectActionData
{
    public required string Tag { get; init; }
    public string Target { get; init; } = default!; // TODO: use
}
