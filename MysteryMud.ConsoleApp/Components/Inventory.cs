using Arch.Core;

namespace MysteryMud.ConsoleApp.Components;

struct Inventory
{
    public List<Entity> Items;

    public Inventory()
    {
        Items = new List<Entity>();
    }
}
