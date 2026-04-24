using TinyECS;

namespace MysteryMud.Domain.Components.Zones;

public struct Zone
{
    public int Id;
    public bool Loaded;
    public List<EntityId> Rooms;
}
