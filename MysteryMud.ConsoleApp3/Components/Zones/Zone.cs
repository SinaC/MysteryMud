using Arch.Core;

namespace MysteryMud.ConsoleApp3.Components.Zones;

struct Zone
{
    public int Id;
    public bool Loaded;
    public List<Entity> Rooms;
}
