using Arch.Core;

namespace MysteryMud.ConsoleApp.Components;

struct RoomEntities
{
    public List<Entity> Entities;

    public RoomEntities()
    {
        Entities = new List<Entity>();
    }
}
