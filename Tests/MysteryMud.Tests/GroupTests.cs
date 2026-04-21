using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class GroupTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly GroupService _groupService;

    public GroupTests()
    {
        _groupService = new GroupService(_f.GameMessage);
    }

    public void Dispose() => _f.Dispose();

    [Fact]
    public void RemoveMember_DisbandsWith_OneMemberLeft()
    {
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").InGroup(group).Build();
        var bob = _f.Player("Bob").InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);

        _groupService.RemoveMember(_f.State, group, alice);

        Assert.False(group.IsAlive());        // group entity destroyed
        Assert.False(bob.Has<GroupMember>()); // last member freed
    }

    [Fact]
    public void LeaderLeaves_OldestMember_BecomesNewLeader()
    {
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").InGroup(group).Build();
        var bob = _f.Player("Bob").InGroup(group).Build();
        var carol = _f.Player("Carol").InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob, carol);

        // alice joined tick 1, bob tick 5, carol tick 10
        alice.Get<GroupMember>().JoinedAtTick = 1;
        bob.Get<GroupMember>().JoinedAtTick = 5;
        carol.Get<GroupMember>().JoinedAtTick = 10;
        group.Get<GroupInstance>().Leader = alice;

        _groupService.RemoveMember(_f.State, group, alice);

        Assert.Equal(bob, group.Get<GroupInstance>().Leader); // bob oldest remaining
    }

    [Fact]
    public void RemoveMember_PromotesNewLeader_WhenLeaderLeaves()
    {
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").InGroup(group).Build();
        var bob = _f.Player("Bob").InGroup(group).Build();
        var carol = _f.Player("Carol").InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob, carol);
        group.Get<GroupInstance>().Leader = alice;

        _groupService.RemoveMember(_f.State, group, alice);

        Assert.Equal(bob, group.Get<GroupInstance>().Leader); // first remaining member promoted
        Assert.True(group.IsAlive());                 // group survives with 2 members
    }

    [Fact]
    public void Disband_RemovesGroupMember_FromAllMembers()
    {
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").InGroup(group).Build();
        var bob = _f.Player("Bob").InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);

        _groupService.Disband(_f.State, group);

        Assert.False(alice.Has<GroupMember>());
        Assert.False(bob.Has<GroupMember>());
        Assert.False(group.IsAlive());
    }

    [Fact]
    public void CombatClaim_GroupReference_ClearedAfterDisband()
    {
        var room = _f.Room().Build();
        var npc = _f.Npc("Orc").WithLocation(room).Build();
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).Build();
        var bob = _f.Player("Bob").WithLocation(room).InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);

        CombatHelpers.AddCombatClaim(npc, alice, 1);
        _groupService.Disband(_f.State, group);

        var claim = npc.Get<CombatInitiator>().Claims.Single();
        Assert.Equal(Entity.Null, claim.ClaimantGroup); // cleared by disband
        Assert.Equal(alice, claim.Claimant);            // personal claim preserved
        Assert.False(claim.Forfeited);
    }
}
