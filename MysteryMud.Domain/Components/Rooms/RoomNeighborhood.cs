using TinyECS;

namespace MysteryMud.Domain.Components.Rooms;

public struct RoomNeighborhood
{
    public List<EntityId> Distance1;
    public List<EntityId> Distance2;
}
