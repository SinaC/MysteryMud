using Arch.Core;

namespace MysteryMud.ConsoleApp.Components;

struct RoomItems
{
    public List<Entity> Items;

    public RoomItems()
    {
        Items = new List<Entity>();
    }
}
