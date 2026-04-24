using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Groups;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Services;

public class GroupService : IGroupService
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public GroupService(World world,  IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void AddMember(GameState state, EntityId group, EntityId member)
    {
        ref var groupInstance = ref _world.Get<GroupInstance>(group);

        groupInstance.Members.Add(member);
        _world.Add(member, new GroupMember
        {
            Group = group,
            JoinedAtTick = state.CurrentTick
        });

        _msg.ToGroup(group).Act("{0} join{0:v} the group.").With(member);
    }

    public void RemoveMember(EntityId group, EntityId member)
    {
        _msg.To(member).Send("You have left the group.");

        ref var groupInstance = ref _world.Get<GroupInstance>(group);
        groupInstance.Members.Remove(member);
        if (_world.Has<GroupMember>(member))
            _world.Remove<GroupMember>(member);

        // forfeit combat claims for this member
        ClearGroupFromClaims(member, group);

        _msg.ToGroup(group).Act("{0} leaves the group.").With(member);

        if (groupInstance.Members.Count == 1)
        {
            Disband(group);
            return;
        }

        // promote oldest remaining member if leader left
        if (groupInstance.Leader == member)
            PromoteNewLeader(group);
    }

    private void PromoteNewLeader(EntityId group)
    {
        ref var groupInstance = ref _world.Get<GroupInstance>(group);

        var newLeader = groupInstance.Members
            .OrderBy(m => _world.Get<GroupMember>(m).JoinedAtTick)
            .First();

        groupInstance.Leader = newLeader;
        _msg.ToGroup(group).Act("{0} {0:b} now the group leader.").With(newLeader);
    }

    public void Disband(EntityId group)
    {
        ref var groupInstance = ref _world.Get<GroupInstance>(group);

        // clear group reference on all active combat claims before destroying group entity
        foreach (var member in groupInstance.Members)
        {
            // find all NPCs this member has claims on and clear the group reference
            // we need to scan — member doesn't track which entities they have claims on
            ClearGroupFromClaims(member, group);
        }

        foreach (var member in groupInstance.Members.ToArray())
        {
            if (_world.Has<GroupMember>(member))
                _world.Remove<GroupMember>(member);
            _msg.To(member).Send("Your group has been disbanded.");
        }

        groupInstance.Members.Clear();
        // destroy group: cannot delete an entity from where -> soft delete
        if (!_world.Has<DisbandedTag>(group))
            _world.Add<DisbandedTag>(group);
    }

    private static readonly QueryDescription _initiatorQueryDesc = new QueryDescription()
        .WithAll<CombatInitiator>();

    private void ClearGroupFromClaims(EntityId member, EntityId group)
    {
        _world.Query(in _initiatorQueryDesc, (EntityId entity, ref CombatInitiator initiator) =>
        {
            for (int i = 0; i < initiator.Claims.Count; i++)
            {
                if (initiator.Claims[i].Claimant == member
                    && initiator.Claims[i].ClaimantGroup == group)
                {
                    initiator.Claims[i] = initiator.Claims[i] with { ClaimantGroup = EntityId.Invalid };
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


    private static void ForfeitAllClaims(EntityId player)
    {
        // TODO
        // Option A — claims are personal, survive disband: player A initiated combat as part of group, group disbands, A still has their personal claim. Simpler, fairer to the player.
        // Option B — claims are tied to group membership, forfeited on disband: if you leave or disband, you lose loot rights. Harsher but avoids exploits (invite someone, get loot rights, disband immediately).
    }
}
