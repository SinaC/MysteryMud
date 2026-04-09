using MysteryMud.Infrastructure.Persistence.Dto.Actions;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class ApplyTagActionData : EffectActionData
{
    public required string Tag { get; init; }
    public string Target { get; init; } = default!; // TODO: use
}
