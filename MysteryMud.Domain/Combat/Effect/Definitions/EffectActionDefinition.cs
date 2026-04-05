using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect.Definitions;

public abstract class EffectActionDefinition
{
    public required TriggerType Trigger { get; init; }
}
