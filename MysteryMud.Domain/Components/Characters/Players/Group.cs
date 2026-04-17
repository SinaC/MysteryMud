using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct Group
{
    public Entity Leader;
    public List<Entity> Members;   // includes leader
    public LootRule LootRule;      // FreeForAll, RoundRobin, MasterLooter, etc.
    public Entity MasterLooter;    // only relevant if LootRule == MasterLooter
}
