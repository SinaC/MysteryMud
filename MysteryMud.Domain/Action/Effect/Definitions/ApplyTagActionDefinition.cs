using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class ApplyTagActionDefinition : EffectActionDefinition
{
    public required EffectTagId EffectTagId { get; init; }
}
