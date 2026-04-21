using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Helpers;

namespace MysteryMud.Domain.Services;

public class GroupService : IGroupService
{
    private readonly IGameMessageService _msg;

    public GroupService(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void AddMember(GameState state, Entity group, Entity member)
    {
        ref var groupInstance = ref group.Get<GroupInstance>();

        groupInstance.Members.Add(member);
        member.Add(new GroupMember
        {
            Group = group,
            JoinedAtTick = state.CurrentTick
        });

        _msg.ToGroup(group).Act("{0} joins the group.").With(member);
    }

    public void RemoveMember(GameState state, Entity group, Entity member)
    {
        _msg.To(member).Send("You have left the group.");

        ref var groupInstance = ref group.Get<GroupInstance>();
        groupInstance.Members.Remove(member);
        if (member.Has<GroupMember>())
            member.Remove<GroupMember>();

        // forfeit combat claims for this member
        ClearGroupFromClaims(state, member, group);

        _msg.ToGroup(group).Act("{0} leaves the group.").With(member);

        if (groupInstance.Members.Count == 1)
        {
            Disband(state, group);
            return;
        }

        // promote oldest remaining member if leader left
        if (groupInstance.Leader == member)
            PromoteNewLeader(group);
    }

    private void PromoteNewLeader(Entity group)
    {
        ref var groupInstance = ref group.Get<GroupInstance>();

        var newLeader = groupInstance.Members
            .OrderBy(m => m.Get<GroupMember>().JoinedAtTick)
            .First();

        groupInstance.Leader = newLeader;
        _msg.ToGroup(group).Act("{0} {0:b} now the group leader.").With(newLeader);
    }

    public void Disband(GameState state, Entity group)
    {
        ref var groupInstance = ref group.Get<GroupInstance>();

        // clear group reference on all active combat claims before destroying group entity
        foreach (var member in groupInstance.Members)
        {
            // find all NPCs this member has claims on and clear the group reference
            // we need to scan — member doesn't track which entities they have claims on
            ClearGroupFromClaims(state, member, group);
        }

        foreach (var member in groupInstance.Members.ToArray())
        {
            if (member.Has<GroupMember>())
                member.Remove<GroupMember>();
            _msg.To(member).Send("Your group has been disbanded.");
        }

        groupInstance.Members.Clear();
        state.World.Destroy(group); // group entity is gone
    }


    private void ClearGroupFromClaims(GameState state, Entity member, Entity group)
    {
        var query = new QueryDescription().WithAll<CombatInitiator>();
        state.World.Query(query, (ref CombatInitiator initiator) =>
        {
            for (int i = 0; i < initiator.Claims.Count; i++)
            {
                if (initiator.Claims[i].Claimant == member
                    && initiator.Claims[i].ClaimantGroup == group)
                {
                    initiator.Claims[i] = initiator.Claims[i] with { ClaimantGroup = Entity.Null };
                }
            }
        });
    }

    // TODO: in a future version we could store claims on player
    // public struct PlayerCombatClaims
    // {
    //      public List<Entity> ClaimedEntities; // NPCs this player has claims on
    // }
    //private static void ClearGroupFromClaims(Entity member, Entity group)
    //{
    //    if (!member.Has<PlayerCombatClaims>()) return;

    //    ref var claims = ref member.Get<PlayerCombatClaims>();
    //    foreach (var claimedEntity in claims.ClaimedEntities)
    //    {
    //        if (!claimedEntity.IsAlive() || !claimedEntity.Has<CombatInitiator>()) continue;

    //        ref var initiator = ref claimedEntity.Get<CombatInitiator>();
    //        for (int i = 0; i < initiator.Claims.Count; i++)
    //        {
    //            if (initiator.Claims[i].Claimant == member
    //                && initiator.Claims[i].ClaimantGroup == group)
    //            {
    //                initiator.Claims[i] = initiator.Claims[i] with { ClaimantGroup = Entity.Null };
    //            }
    //        }
    //    }
    //}


    private static void ForfeitAllClaims(Entity player)
    {
        // TODO
        // Option A — claims are personal, survive disband: player A initiated combat as part of group, group disbands, A still has their personal claim. Simpler, fairer to the player.
        // Option B — claims are tied to group membership, forfeited on disband: if you leave or disband, you lose loot rights. Harsher but avoids exploits (invite someone, get loot rights, disband immediately).
    }
}
