using DefaultEcs;

namespace MysteryMud.Domain.Components.Rooms;

public struct RoomContents
{
    public List<Entity> Characters;
    public List<Entity> Items;
}
