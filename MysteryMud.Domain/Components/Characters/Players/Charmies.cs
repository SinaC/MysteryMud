using TinyECS;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct Charmies
{
    public List<EntityId> Entities; // charmed mobs controlled by this master
}
