using Arch.Core;

namespace MysteryMud.ConsoleApp2.ECS.Components.Rooms;

public struct RoomContents
{
    public List<Entity> Characters;
    public List<Entity> Items;
}
