using TinyECS;

namespace MysteryMud.GameData.Definitions;

public struct EffectData
{
    public int EffectId;

    public EntityId Source;
    public EntityId Target;

    public bool IsHarmful;

    // TODO: add EffectTrigger (Hit, Tick, ...) ?
    public int EffectiveDamageAmount; // set if this effect is triggered from damage dealt such as weapon proc
}
