using Arch.Core;

namespace MysteryMud.GameData.Definitions;

public struct EffectData
{
    public int EffectId;

    public Entity Source;
    public Entity Target;

    // TODO: add EffectTrigger (Hit, Tick, ...) ?
    public int EffectiveDamageAmount; // set if this effect is triggered from damage dealt such as weapon proc
}
