using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Groups;

public struct GroupInstance
{
    public Entity Leader;
    public List<Entity> Members;   // includes leader
    public LootRule LootRule;      // FreeForAll, RoundRobin, MasterLooter, etc.
    public Entity MasterLooter;    // only relevant if LootRule == MasterLooter
}
