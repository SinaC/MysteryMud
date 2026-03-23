using Arch.Core;

namespace MysteryMud.ConsoleApp3.Domain.Components.Characters;

struct CharacterEffects
{
    public List<Entity> Effects;
    public Entity?[] EffectsByTag; // fixed array for O(1)
    public ulong ActiveTags; // bitfield of active EffectTagIds for quick lookup
}
