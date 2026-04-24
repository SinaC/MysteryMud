using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.Tests.Infrastructure;
using TinyECS;

namespace MysteryMud.Tests;

public class FollowSystemTests : IDisposable
{
    private readonly MudTestFixture _f = new();

    public void Dispose() => _f.Dispose();

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private EntityId MakeRoom() => _f.Room().Build();

    private EntityId MakeLinkedRoom(EntityId fromRoom, DirectionKind dir)
    {
        var toRoom = MakeRoom();
        RoomFactory.LinkRoom(_f.World, fromRoom, toRoom, dir);
        return toRoom;
    }

    private EntityId MakePlayer(EntityId room, string name = "Player") =>
        _f.Player(name).WithLocation(room).Build();

    private EntityId MakeNpc(EntityId room, string name = "Mob") =>
        _f.Npc(name).WithLocation(room).Build();

    private FollowSystem MakeSystem() =>
        new(_f.World, _f.GameMessage, _f.Intents);

    private void QueueMove(EntityId actor, EntityId from, EntityId to, DirectionKind dir)
    {
        ref var intent = ref _f.Intents.Move.Add();
        intent.Actor = actor;
        intent.FromRoom = from;
        intent.ToRoom = to;
        intent.Direction = dir;
        intent.AutoLook = true;
    }

    private bool HasFollowIntent(EntityId follower, EntityId expectedTo, DirectionKind expectedDir) =>
        _f.Intents.MoveSpan.ToArray().Any(i =>
            i.Actor == follower &&
            i.ToRoom == expectedTo &&
            i.Direction == expectedDir);

    private void AssertFollowedWith(EntityId follower, EntityId expectedTo, DirectionKind expectedDir) =>
        Assert.True(
            HasFollowIntent(follower, expectedTo, expectedDir),
            $"{_f.World.Get<Name>(follower).Value} should have a follow MoveIntent to {expectedTo} going {expectedDir}");

    private void AssertNoFollowIntent(EntityId follower) =>
        Assert.False(
            _f.Intents.MoveSpan.ToArray().Any(i => i.Actor == follower),
            $"{_f.World.Get<Name>(follower).Value} should not have a MoveIntent queued by FollowSystem");

    // ------------------------------------------------------------------
    // Basic follow
    // ------------------------------------------------------------------
    [Fact]
    public void Follower_GetsMatchingMoveIntent_WhenLeaderMoves()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.North);
        var leader = MakePlayer(roomA, "Leader");
        var follower = MakePlayer(roomA, "Follower");
        _f.World.Add(follower, new Following { Leader = leader }); 

        QueueMove(leader, roomA, roomB, DirectionKind.North);
        MakeSystem().Tick(_f.State);

        AssertFollowedWith(follower, roomB, DirectionKind.North);
    }

    [Fact]
    public void NonFollower_DoesNotGetMoveIntent()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.North);
        var leader = MakePlayer(roomA, "Leader");
        var bystander = MakePlayer(roomA, "Bystander");

        QueueMove(leader, roomA, roomB, DirectionKind.North);
        MakeSystem().Tick(_f.State);

        AssertNoFollowIntent(bystander);
    }

    // ------------------------------------------------------------------
    // Charmie follow
    // ------------------------------------------------------------------
    [Fact]
    public void Charmie_FollowsMaster_WhenMasterMoves()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.East);
        var master = MakePlayer(roomA, "Master");
        var charmie = MakeNpc(roomA, "Pet");
        _f.World.Add(charmie, new Charmed { Master = master });

        QueueMove(master, roomA, roomB, DirectionKind.East);
        MakeSystem().Tick(_f.State);

        AssertFollowedWith(charmie, roomB, DirectionKind.East);
    }

    [Fact]
    public void Charmie_WithoutCharmed_DoesNotFollowMaster()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.East);
        var master = MakePlayer(roomA, "Master");
        var npc = MakeNpc(roomA, "RandomMob");

        QueueMove(master, roomA, roomB, DirectionKind.East);
        MakeSystem().Tick(_f.State);

        AssertNoFollowIntent(npc);
    }

    // ------------------------------------------------------------------
    // Chained follow
    // ------------------------------------------------------------------
    [Fact]
    public void ChainedFollow_AllMembersGetMoveIntent()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.South);
        var a = MakePlayer(roomA, "A");
        var b = MakePlayer(roomA, "B");
        var c = MakePlayer(roomA, "C");
        _f.World.Add(b, new Following { Leader = a });
        _f.World.Add(c, new Following { Leader = b });

        QueueMove(a, roomA, roomB, DirectionKind.South);
        MakeSystem().Tick(_f.State);

        AssertFollowedWith(b, roomB, DirectionKind.South);
        AssertFollowedWith(c, roomB, DirectionKind.South);
    }

    [Fact]
    public void ChainedFollow_BreaksIfMiddleMemberIsInCombat()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.South);
        var a = MakePlayer(roomA, "A");
        var b = MakePlayer(roomA, "B");
        var c = MakePlayer(roomA, "C");
        _f.World.Add(b,  new Following { Leader = a });
        _f.World.Add(c, new Following { Leader = b });
        _f.World.Add(b, new CombatState());

        QueueMove(a, roomA, roomB, DirectionKind.South);
        MakeSystem().Tick(_f.State);

        AssertNoFollowIntent(b);
        AssertNoFollowIntent(c);
    }

    // ------------------------------------------------------------------
    // Follower already moved themselves this tick
    // ------------------------------------------------------------------
    [Fact]
    public void Follower_WithOwnMoveIntent_IsNotOverridden()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.North);
        var roomC = MakeLinkedRoom(roomA, DirectionKind.West);
        var leader = MakePlayer(roomA, "Leader");
        var follower = MakePlayer(roomA, "Follower");
        _f.World.Add(follower, new Following { Leader = leader });

        QueueMove(leader, roomA, roomB, DirectionKind.North);
        QueueMove(follower, roomA, roomC, DirectionKind.West);  // own move queued before system runs

        MakeSystem().Tick(_f.State);

        // Own intent preserved, no extra intent added for leader's direction
        Assert.False(HasFollowIntent(follower, roomB, DirectionKind.North));
        Assert.True(HasFollowIntent(follower, roomC, DirectionKind.West));
    }

    // ------------------------------------------------------------------
    // Blocking states
    // ------------------------------------------------------------------
    [Fact]
    public void Follower_InCombat_DoesNotFollow()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.North);
        var leader = MakePlayer(roomA, "Leader");
        var follower = MakePlayer(roomA, "Follower");
        _f.World.Add(follower, new Following { Leader = leader });
        _f.World.Add(follower, new CombatState());

        QueueMove(leader, roomA, roomB, DirectionKind.North);
        MakeSystem().Tick(_f.State);

        AssertNoFollowIntent(follower);
    }

    // TODO
    //[Fact]
    //public void Follower_Stunned_DoesNotFollow()
    //{
    //    var roomA = MakeRoom();
    //    var roomB = MakeLinkedRoom(roomA, DirectionKind.North);
    //    var leader = MakePlayer(roomA, "Leader");
    //    var follower = MakePlayer(roomA, "Follower");
    //    follower.Add(new Following { Leader = leader });
    //    follower.Add(new Stunned());

    //    QueueMove(leader, roomA, roomB, DirectionKind.North);
    //    MakeSystem().Tick(_f.State);

    //    AssertNoFollowIntent(follower);
    //}

    // ------------------------------------------------------------------
    // Room separation
    // ------------------------------------------------------------------
    [Fact]
    public void Follower_InDifferentRoom_DoesNotFollow()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.North);
        var roomC = MakeRoom();
        var leader = MakePlayer(roomA, "Leader");
        var follower = MakePlayer(roomC, "Follower");
        _f.World.Add(follower, new Following { Leader = leader });

        QueueMove(leader, roomA, roomB, DirectionKind.North);
        MakeSystem().Tick(_f.State);

        AssertNoFollowIntent(follower);
    }

    // ------------------------------------------------------------------
    // Blocked entry
    // ------------------------------------------------------------------
    [Fact]
    public void Follower_BlockedByClosedDoor_GetsMessageAndNoIntent()
    {
        var roomA = MakeRoom();
        var roomB = MakeRoom();
        ref var graph = ref _f.World.Get<RoomGraph>(roomA);
        graph.Exits[DirectionKind.North] = new Exit { Direction = DirectionKind.North, TargetRoom = roomB, Closed = true };

        var leader = MakePlayer(roomA, "Leader");
        var follower = MakePlayer(roomA, "Follower");
        _f.World.Add(follower, new Following { Leader = leader });

        QueueMove(leader, roomA, roomB, DirectionKind.North);
        MakeSystem().Tick(_f.State);

        AssertNoFollowIntent(follower);
        Assert.True(_f.GameMessage.HasMessageFor(follower));
    }

    // ------------------------------------------------------------------
    // No leader movement this tick
    // ------------------------------------------------------------------
    [Fact]
    public void NoLeaderMove_ProducesNoFollowerIntent()
    {
        var roomA = MakeRoom();
        var leader = MakePlayer(roomA, "Leader");
        var follower = MakePlayer(roomA, "Follower");
        _f.World.Add(follower, new Following { Leader = leader });

        MakeSystem().Tick(_f.State);

        AssertNoFollowIntent(follower);
    }

    // ------------------------------------------------------------------
    // Cycle guard
    // ------------------------------------------------------------------
    [Fact]
    public void CyclicFollow_DoesNotHang()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.North);
        var a = MakePlayer(roomA, "A");
        var b = MakePlayer(roomA, "B");
        _f.World.Add(a, new Following { Leader = b });
        _f.World.Add(b, new Following { Leader = a });

        QueueMove(a, roomA, roomB, DirectionKind.North);

        var ex = Record.Exception(() => MakeSystem().Tick(_f.State));
        Assert.Null(ex);
    }

    // ------------------------------------------------------------------
    // Multiple followers + charmie all follow same leader
    // ------------------------------------------------------------------
    [Fact]
    public void MultipleFollowers_AllGetMoveIntent()
    {
        var roomA = MakeRoom();
        var roomB = MakeLinkedRoom(roomA, DirectionKind.East);
        var leader = MakePlayer(roomA, "Leader");
        var followerA = MakePlayer(roomA, "FollowerA");
        var followerB = MakePlayer(roomA, "FollowerB");
        var charmie = MakeNpc(roomA, "Pet");
        _f.World.Add(followerA, new Following { Leader = leader });
        _f.World.Add(followerB, new Following { Leader = leader });
        _f.World.Add(charmie, new Charmed { Master = leader });

        QueueMove(leader, roomA, roomB, DirectionKind.East);
        MakeSystem().Tick(_f.State);

        AssertFollowedWith(followerA, roomB, DirectionKind.East);
        AssertFollowedWith(followerB, roomB, DirectionKind.East);
        AssertFollowedWith(charmie, roomB, DirectionKind.East);
    }
}