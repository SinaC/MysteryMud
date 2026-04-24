using TinyECS;

namespace MysteryMud.Domain.Components.Characters.Mobiles;

public struct ThreatTable
{
    public Dictionary<EntityId, long> Threat;
    public long LastUpdateTick;
}
