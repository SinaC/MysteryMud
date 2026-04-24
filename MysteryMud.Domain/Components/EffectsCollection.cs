using TinyECS;

namespace MysteryMud.Domain.Components;

public struct EffectsCollection
{
    public List<EntityId> Effects;
    public List<EntityId>?[] EffectsByTag; // fixed array for O(1)
    public ulong ActiveTags;// bitfield of active EffectTagIds for quick lookup
}
