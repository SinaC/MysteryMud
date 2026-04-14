using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class ApplyCharacterTagActionDefinition : EffectActionDefinition
{
    public required CharacterEffectTagId EffectTagId { get; init; }
}
