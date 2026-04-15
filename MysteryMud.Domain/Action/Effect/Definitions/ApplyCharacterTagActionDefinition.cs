using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Character)]
public class ApplyCharacterTagActionDefinition : EffectActionDefinition
{
    public required CharacterEffectTagId EffectTagId { get; init; }
}
