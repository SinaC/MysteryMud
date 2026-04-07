using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public abstract class EffectActionDefinition
{
    public required TriggerType Trigger { get; init; }
}
