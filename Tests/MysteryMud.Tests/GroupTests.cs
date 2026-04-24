using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.Tests.Infrastructure;
using TinyECS;

namespace MysteryMud.Tests;

public class GroupTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly GroupService _groupService;

    public GroupTests()
    {
        _groupService = new GroupService(_f.World, _f.GameMessage);
    }

    public void Dispose() => _f.Dispose();

    [Fact]
    public void RemoveMember_DisbandsWith_OneMemberLeft()
    {
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").InGroup(group).Build();
        var bob = _f.Player("Bob").InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);

        _groupService.RemoveMember(group, alice);

        Assert.True(_f.World.Has<DisbandedTag>(group)); // group entity destroyed
        Assert.False(_f.World.Has<GroupMember>(bob));   // last member freed
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
        _f.World.Get<GroupMember>(alice).JoinedAtTick = 1;
        _f.World.Get<GroupMember>(bob).JoinedAtTick = 5;
        _f.World.Get<GroupMember>(carol).JoinedAtTick = 10;
        _f.World.Get<GroupInstance>(group).Leader = alice;

        _groupService.RemoveMember(group, alice);

        Assert.Equal(bob, _f.World.Get<GroupInstance>(group).Leader); // bob oldest remaining
    }

    [Fact]
    public void RemoveMember_PromotesNewLeader_WhenLeaderLeaves()
    {
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").InGroup(group).Build();
        var bob = _f.Player("Bob").InGroup(group).Build();
        var carol = _f.Player("Carol").InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob, carol);
        _f.World.Get<GroupInstance>(group).Leader = alice;

        _groupService.RemoveMember(group, alice);

        Assert.Equal(bob, _f.World.Get<GroupInstance>(group).Leader); // first remaining member promoted
        Assert.False(_f.World.Has<DisbandedTag>(group));              // group survives with 2 members
    }

    [Fact]
    public void Disband_RemovesGroupMember_FromAllMembers()
    {
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").InGroup(group).Build();
        var bob = _f.Player("Bob").InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);

        _groupService.Disband(group);

        Assert.False(_f.World.Has<GroupMember>(alice));
        Assert.False(_f.World.Has<GroupMember>(bob));
        Assert.True(_f.World.Has<DisbandedTag>(group));
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

        CombatHelpers.AddCombatClaim(_f.World, _f.State, npc, alice);
        _groupService.Disband(group);

        var claim = _f.World.Get<CombatInitiator>(npc).Claims.Single();
        Assert.Equal(EntityId.Invalid, claim.ClaimantGroup); // cleared by disband
        Assert.Equal(alice, claim.Claimant);            // personal claim preserved
        Assert.False(claim.Forfeited);
    }
}
