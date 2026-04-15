using Arch.Core;

namespace MysteryMud.Domain.Components;

public struct EffectsCollection
{
    public List<Entity> Effects;
    public List<Entity>?[] EffectsByTag; // fixed array for O(1)
    public ulong ActiveTags;// bitfield of active EffectTagIds for quick lookup
}
