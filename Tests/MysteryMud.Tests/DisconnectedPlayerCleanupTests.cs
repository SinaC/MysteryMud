using DefaultEcs;
using Microsoft.Extensions.Logging.Abstractions;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.Infrastructure.Persistence;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class DisconnectedPlayerCleanupTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly CleanupSystem _sut;
    private readonly CombatService _combatService;

    public DisconnectedPlayerCleanupTests()
    {
        FollowService followService = new(_f.GameMessage);
        GroupService groupService = new(_f.World, _f.GameMessage);
        _combatService = new CombatService(_f.World);
        DirtyTracker dirtyTracker = new();
        EffectLifecycleManager effectLifecycleManager = new(dirtyTracker);

        _sut = new CleanupSystem(
            _f.World,
            NullLogger.Instance,
            followService,
            groupService,
            _combatService,
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
        alice.Set(new Followers { Entities = [bob] });

        bob.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(bob.IsAlive);
        Assert.DoesNotContain(bob, alice.Get<Followers>().Entities);
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
        alice.Get<Followers>().Entities.AddRange([bob, carol]);

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(alice.IsAlive);
        Assert.False(bob.Has<Following>());
        Assert.False(carol.Has<Following>());
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
        group.Get<GroupInstance>().Leader = alice;

        bob.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(bob.IsAlive);
        Assert.True(group.Has<DisbandedTag>()); // group disbanded — only alice remained
        Assert.False(alice.Has<GroupMember>()); // alice freed from group
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
        alice.Get<GroupMember>().JoinedAtTick = 1;
        bob.Get<GroupMember>().JoinedAtTick = 5;
        carol.Get<GroupMember>().JoinedAtTick = 10;
        group.Get<GroupInstance>().Leader = alice;

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.Equal(bob, group.Get<GroupInstance>().Leader); // oldest remaining
    }

    [Fact]
    public void DisconnectedPlayer_LastMemberInGroup_DisbandGroup()
    {
        var room = _f.Room().Build();
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).Build();
        var bob = _f.Player("Bob").WithLocation(room).InGroup(group).Build();
        _f.AddGroupMembers(group, alice, bob);

        alice.Set<DisconnectedTag>();
        bob.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.True(group.Has<DisbandedTag>());
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
        alice.Set(new CombatState { Target = orc });
        orc.Set(new CombatState { Target = alice });

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(alice.IsAlive);
        Assert.False(orc.Has<CombatState>()); // orc no longer targeting disconnected player
    }

    [Fact]
    public void DisconnectedPlayer_CombatState_RemovedFromMultipleAttackers()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room).Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room).Build();
        alice.Set(new CombatState { Target = orc1 });
        orc1.Set(new CombatState { Target = alice });
        orc2.Set(new CombatState { Target = alice });

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(orc1.Has<CombatState>());
        Assert.False(orc2.Has<CombatState>());
    }

    [Fact]
    public void DisconnectedPlayer_NewCombatantTag_Removed()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        alice.Set(new CombatState { Target = default });
        alice.Set<NewCombatantTag>();

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(alice.IsAlive); // entity destroyed, tag implicitly gone
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
                      .With(new ThreatTable { Entries = new Dictionary<Entity, decimal> { [alice] = 100 } })
                      .Build();
        orc.Set<ActiveThreatTag>();

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(orc.Get<ThreatTable>().Entries.ContainsKey(alice));
    }

    [Fact]
    public void DisconnectedPlayer_IsRemovedFromMultipleThreatTables()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room)
                      .With(new ThreatTable { Entries = new Dictionary<Entity, decimal> { [alice] = 100 } })
                      .Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room)
                      .With(new ThreatTable { Entries = new Dictionary<Entity, decimal> { [alice] = 50 } })
                      .Build();
        orc1.Set<ActiveThreatTag>();
        orc2.Set<ActiveThreatTag>();

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(orc1.Get<ThreatTable>().Entries.ContainsKey(alice));
        Assert.False(orc2.Get<ThreatTable>().Entries.ContainsKey(alice));
    }

    [Fact]
    public void DisconnectedPlayer_NpcWithoutActiveThreatTag_NotScanned()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                      .With(new ThreatTable { Entries = new Dictionary<Entity, decimal> { [alice] = 100 } })
                      .Build();
        // no ActiveThreatTag — should not be scanned

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        // threat table untouched since orc has no ActiveThreatTag
        Assert.True(orc.Get<ThreatTable>().Entries.ContainsKey(alice));
    }

    // -------------------------------------------------------------------------
    // Room contents
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_IsRemovedFromRoomContents()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.DoesNotContain(alice, room.Get<RoomContents>().Characters);
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
        _combatService.AddCombatClaim(orc, alice, _f.State.CurrentTick);

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        // orc still alive, claim forfeited
        Assert.True(orc.Get<CombatInitiator>().Claims[0].Forfeited);
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
        effect.Set(new EffectInstance { Target = alice });
        alice.Get<CharacterEffects>().Data.Effects.Add(effect);

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(effect.IsAlive);
    }

    // -------------------------------------------------------------------------
    // Entity destroyed
    // -------------------------------------------------------------------------

    [Fact]
    public void DisconnectedPlayer_IsDestroyedAfterCleanup()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();

        alice.Set<DisconnectedTag>();
        _sut.Tick(_f.State);

        Assert.False(alice.IsAlive);
    }

    public void Dispose() => _f.Dispose();
}
