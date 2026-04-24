using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Groups;

public struct GroupInstance
{
    public EntityId Leader;
    public List<EntityId> Members;   // includes leader
    public LootRule LootRule;      // FreeForAll, RoundRobin, MasterLooter, etc.
    public EntityId MasterLooter;    // only relevant if LootRule == MasterLooter
}
