using Arch.Core;

namespace MysteryMud.Domain.Components.Characters;

public struct CharacterEffects
{
    public List<Entity> Effects;
    public Entity?[] EffectsByTag; // fixed array for O(1)
    public ulong ActiveTags; // bitfield of active EffectTagIds for quick lookup
}
