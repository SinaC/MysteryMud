using TinyECS;

namespace MysteryMud.GameData.Events;

public struct EffectResolvedEvent
{
    public EntityId Source;
    public EntityId Target;
    public int EffectId;
    // TODO: EffectContext ?
}
