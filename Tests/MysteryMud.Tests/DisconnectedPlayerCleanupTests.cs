using Microsoft.Extensions.Logging.Abstractions;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.Infrastructure.Persistence;
using MysteryMud.Tests.Infrastructure;
using TinyECS;

namespace MysteryMud.Tests;

public class DisconnectedPlayerCleanupTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly CleanupSystem _sut;

    public DisconnectedPlayerCleanupTests()
    {
        FollowService followService = new(_f.World, _f.GameMessage);
        GroupService groupService = new(_f.World, _f.GameMessage);
        DirtyTracker dirtyTracker = new();
        EffectLifecycleManager effectLifecycleManager = new(_f.World, dirtyTracker);

        _sut = new CleanupSystem(
            _f.World,
            NullLogger.Instance,
            followService,
            groupService,
            effectLifecycleManager);
    }

    // -------------------------------------------------------------------------
    // Follow
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_StopsFollowing()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room)
                       .With(new Following { Leader = alice })
                       .Build();
        _f.World.Add(alice,new Followers { Entities = [bob] });

        _f.World.Add<DisconnectedTag>(bob);
        _sut.Tick(_f.State);

        Assert.False(_f.World.IsAlive(bob));
        Assert.DoesNotContain(bob, _f.World.Get<Followers>(alice).Entities);
    }

    [Fact]
    public void DisconnectedPlayer_WhoIsBeingFollowed_FollowersStopped()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room)
                      .With(new Followers { Entities = [] })
                      .Build();
        var bob = _f.Player("Bob").WithLocation(room)
                      .With(new Following { Leader = alice })
                      .Build();
        var carol = _f.Player("Carol").WithLocation(room)
                      .With(new Following { Leader = alice })
                      .Build();
        _f.World.Get<Followers>(alice).Entities.AddRange([bob, carol]);

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.IsAlive(alice));
        Assert.False(_f.World.Has<Following>(bob));
        Assert.False(_f.World.Has<Following>(carol));
    }

    // -------------------------------------------------------------------------
    // Group
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_IsRemovedFromGroup()
    {
        var room = _f.Room().Build();
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).Build();
        var bob = _f.Player("Bob").WithLocation(room).InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);
        _f.World.Get<GroupInstance>(group).Leader = alice;

        _f.World.Add<DisconnectedTag>(bob);
        _sut.Tick(_f.State);

        Assert.False(_f.World.IsAlive(bob));
        Assert.True(_f.World.Has<DisbandedTag>(group)); // group disbanded — only alice remained
        Assert.False(_f.World.Has<GroupMember>(alice)); // alice freed from group
    }

    [Fact]
    public void DisconnectedPlayer_WasGroupLeader_NewLeaderPromoted()
    {
        var room = _f.Room().Build();
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).Build();
        var bob = _f.Player("Bob").WithLocation(room).InGroup(group).Build();
        var carol = _f.Player("Carol").WithLocation(room).InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob, carol);
        _f.World.Get<GroupMember>(alice).JoinedAtTick = 1;
        _f.World.Get<GroupMember>(bob).JoinedAtTick = 5;
        _f.World.Get<GroupMember>(carol).JoinedAtTick = 10;
        _f.World.Get<GroupInstance>(group).Leader = alice;

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.Equal(bob, _f.World.Get<GroupInstance>(group).Leader); // oldest remaining
    }

    [Fact]
    public void DisconnectedPlayer_LastMemberInGroup_DisbandGroup()
    {
        var room = _f.Room().Build();
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).Build();
        var bob = _f.Player("Bob").WithLocation(room).InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);

        _f.World.Add<DisconnectedTag>(alice);
        _f.World.Add<DisconnectedTag>(bob);
        _sut.Tick(_f.State);

        Assert.True(_f.World.Has<DisbandedTag>(group));
    }

    // -------------------------------------------------------------------------
    // Combat
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_IsRemovedFromCombat()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        _f.World.Add(alice,new CombatState { Target = orc });
        _f.World.Add(orc, new CombatState { Target = alice });

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.IsAlive(alice));
        Assert.False(_f.World.Has<CombatState>(orc)); // orc no longer targeting disconnected player
    }

    [Fact]
    public void DisconnectedPlayer_CombatState_RemovedFromMultipleAttackers()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room).Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room).Build();
        _f.World.Add(alice,new CombatState { Target = orc1 });
        _f.World.Add(orc1, new CombatState { Target = alice });
        _f.World.Add(orc2, new CombatState { Target = alice });

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.Has<CombatState>(orc1));
        Assert.False(_f.World.Has<CombatState>(orc2));
    }

    [Fact]
    public void DisconnectedPlayer_NewCombatantTag_Removed()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        _f.World.Add(alice,new CombatState { Target = EntityId.Invalid });
        _f.World.Add<NewCombatantTag>(alice);

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.IsAlive(alice)); // entity destroyed, tag implicitly gone
    }

    // -------------------------------------------------------------------------
    // Threat table
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_IsRemovedFromThreatTable()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 100 } })
                      .Build();
        _f.World.Add<ActiveThreatTag>(orc);

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.Get<ThreatTable>(orc).Threat.ContainsKey(alice));
    }

    [Fact]
    public void DisconnectedPlayer_IsRemovedFromMultipleThreatTables()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 100 } })
                      .Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 50 } })
                      .Build();
        _f.World.Add<ActiveThreatTag>(orc1);
        _f.World.Add<ActiveThreatTag>(orc2);

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.Get<ThreatTable>(orc1).Threat.ContainsKey(alice));
        Assert.False(_f.World.Get<ThreatTable>(orc2).Threat.ContainsKey(alice));
    }

    [Fact]
    public void DisconnectedPlayer_NpcWithoutActiveThreatTag_NotScanned()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 100 } })
                      .Build();
        // no ActiveThreatTag — should not be scanned

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        // threat table untouched since orc has no ActiveThreatTag
        Assert.True(_f.World.Get<ThreatTable>(orc).Threat.ContainsKey(alice));
    }

    // -------------------------------------------------------------------------
    // Room contents
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_IsRemovedFromRoomContents()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.DoesNotContain(alice, _f.World.Get<RoomContents>(room).Characters);
    }

    // -------------------------------------------------------------------------
    // Combat claims
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_CombatClaim_Forfeited()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        CombatHelpers.AddCombatClaim(_f.World, _f.State, orc, alice);

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        // orc still alive, claim forfeited
        Assert.True(_f.World.Get<CombatInitiator>(orc).Claims[0].Forfeited);
    }

    // -------------------------------------------------------------------------
    // Effects
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_Effects_AreRemoved()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var effect = _f.World.CreateEntity();
        _f.World.Add(effect, new EffectInstance { Target = alice });
        _f.World.Get<CharacterEffects>(alice).Data.Effects.Add(effect);

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.IsAlive(effect));
    }

    // -------------------------------------------------------------------------
    // Entity destroyed
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_IsDestroyedAfterCleanup()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();

        _f.World.Add<DisconnectedTag>(alice);
        _sut.Tick(_f.State);

        Assert.False(_f.World.IsAlive(alice));
    }

    public void Dispose() => _f.Dispose();
}
