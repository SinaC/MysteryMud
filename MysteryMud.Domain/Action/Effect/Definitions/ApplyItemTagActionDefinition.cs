using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Item)]
public class ApplyItemTagActionDefinition : EffectActionDefinition
{
    public required ItemEffectTagId EffectTagId { get; init; }
}
