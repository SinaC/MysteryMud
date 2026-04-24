using TinyECS;

namespace MysteryMud.Domain.Components.Rooms;

public struct RoomContents
{
    public List<EntityId> Characters;
    public List<EntityId> Items;
}
