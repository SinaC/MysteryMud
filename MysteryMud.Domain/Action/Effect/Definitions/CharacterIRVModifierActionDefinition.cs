using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Character | EffectTargetKind.Item)]
public class CharacterIRVModifierActionDefinition : EffectActionDefinition
{
    public required FlagModifierKind Modifier { get; init; }
    public required IRVLocation Location { get; init; }
    public required ulong DamageKinds { get; init; }
}
