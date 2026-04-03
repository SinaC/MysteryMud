using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct EffectResolvedEvent
{
    public Entity Source;
    public Entity Target;
    public int EffectId;
    // TODO: EffectContext ?

    public bool Cancelled;
}
