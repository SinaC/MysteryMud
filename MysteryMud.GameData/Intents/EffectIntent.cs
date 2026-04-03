using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct EffectIntent
{
    public Entity Source;
    public Entity Target;
    public int EffectId;
    // TODO: EffectContext ?

    public bool Cancelled;
}
