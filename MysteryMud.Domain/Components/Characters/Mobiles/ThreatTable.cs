using DefaultEcs;

namespace MysteryMud.Domain.Components.Characters.Mobiles;

public struct ThreatTable
{
    public Dictionary<Entity, long> Threat;
    public long LastUpdateTick;
}
