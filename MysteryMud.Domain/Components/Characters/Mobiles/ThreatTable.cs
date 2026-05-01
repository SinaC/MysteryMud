using DefaultEcs;

namespace MysteryMud.Domain.Components.Characters.Mobiles;

public struct ThreatTable
{
    public Dictionary<Entity, decimal> Entries;
    public long LastUpdateTick;
}
