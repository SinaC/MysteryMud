using DefaultEcs;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class FleeSystemTests : IDisposable
{
    private readonly MudTestFixture _f;
    private readonly CombatService _combatService;
    private readonly TestEventBuffer<FleeBlockedEvent> _fleeBlockedEvents = new();

    public FleeSystemTests()
    {
        _f = new MudTestFixture();
        _combatService = new CombatService(_f.World);
    }

    public void Dispose() => _f.Dispose();

    private FleeSystem CreateSystem(IRandom random)
        => new(random, _f.GameMessage, _combatService, _f.TestExperienceService, _f.Intents, _fleeBlockedEvents);

    // Helpers

    private (Entity player, Entity room) CreatePlayerInRoom()
    {
        var room = _f.Room().Build();
        var player = _f.Player()
            .WithLocation(room)
            .Build();
        return (player, room);
    }

    private Entity CreateConnectedRoom(Entity fromRoom, DirectionKind direction)
    {
        var toRoom = _f.Room().Build();
        ref var graph = ref fromRoom.Get<RoomGraph>();
        graph.Exits[direction] = new Exit { Direction = direction, TargetRoom = toRoom };
        return toRoom;
    }

    private void PutInCombat(Entity attacker, Entity target)
    {
        attacker.Set(new CombatState { Target = target });
        target.Set(new CombatState { Target = attacker });
    }

    private void QueueFlee(Entity entity, Entity fromRoom)
    {
        ref var flee = ref _f.Intents.Flee.Add();
        flee.Entity = entity;
        flee.FromRoom = fromRoom;
    }

    // direction index 0=North, 1=South, 2=East, 3=West
    // Pick uses Next(0,4), so value/4.0 selects the index
    private static double DirectionValue(DirectionKind dir) => (int)dir / 4.0;

    // Tests

    [Fact]
    public void Flee_NotInCombat_EmitsFleeBlockedEvent()
    {
        var (player, room) = CreatePlayerInRoom();
        QueueFlee(player, room);

        var system = CreateSystem(new FixedRandom());
        system.Tick(_f.State);

        var evt = Assert.Single(_fleeBlockedEvents);
        Assert.Equal(player, evt.Entity);
        Assert.Equal(FleeBlockedReason.NotInCombat, evt.Reason);
    }

    [Fact]
    public void Flee_NotInCombat_DoesNotEmitMoveIntent()
    {
        var (player, room) = CreatePlayerInRoom();
        QueueFlee(player, room);

        var system = CreateSystem(new FixedRandom());
        system.Tick(_f.State);

        Assert.Empty(_f.Intents.Move.Span.ToArray());
    }

    [Fact]
    public void Flee_NoExits_EmitsNoExitBlockedEvent()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var system = CreateSystem(new FixedRandom());
        system.Tick(_f.State);

        var evt = Assert.Single(_fleeBlockedEvents);
        Assert.Equal(FleeBlockedReason.NoExit, evt.Reason);
    }

    [Fact]
    public void Flee_NoExits_SendsPanicMessage()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var system = CreateSystem(new FixedRandom());
        system.Tick(_f.State);

        Assert.Contains(_f.GameMessage.GetMessagesFor(player), m => m.Contains("PANIC"));
    }

    [Fact]
    public void Flee_AllAttemptsPickMissingDirection_EmitsFailedToFlee()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        // Only North exit exists, but all 6 picks are South
        CreateConnectedRoom(room, DirectionKind.North);
        var southPicks = Enumerable.Repeat(DirectionValue(DirectionKind.South), 6).ToArray();

        var system = CreateSystem(new FixedRandom(southPicks));
        system.Tick(_f.State);

        var evt = Assert.Single(_fleeBlockedEvents);
        Assert.Equal(FleeBlockedReason.FailedToFlee, evt.Reason);
    }

    [Fact]
    public void Flee_Success_EmitsMoveIntent()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var toRoom = CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_f.State);

        var move = Assert.Single(_f.Intents.Move.Span.ToArray());
        Assert.Equal(player, move.Actor);
        Assert.Equal(room, move.FromRoom);
        Assert.Equal(toRoom, move.ToRoom);
        Assert.True(move.AutoLook);
    }

    [Fact]
    public void Flee_Success_RemovesPlayerFromCombat()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_f.State);

        Assert.False(player.Has<CombatState>());
    }

    [Fact]
    public void Flee_Success_GrantsNegativeExperience()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_f.State);

        Assert.Equal(-10, _f.TestExperienceService.LastGranted);
    }

    [Fact]
    public void Flee_Success_SendsFleeMessages()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_f.State);

        Assert.Contains(_f.GameMessage.GetMessagesFor(player), m => m.Contains("flee"));
        Assert.Contains(_f.GameMessage.GetMessagesFor(npc), m => m.Contains("fled"));
    }

    [Fact]
    public void Flee_Success_FirstAttemptMisses_SecondSucceeds()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _f.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var toRoom = CreateConnectedRoom(room, DirectionKind.North);
        // First pick: South (no exit), second pick: North (valid)
        var system = CreateSystem(new FixedRandom(
            DirectionValue(DirectionKind.South),
            DirectionValue(DirectionKind.North)));
        system.Tick(_f.State);

        var move = Assert.Single(_f.Intents.Move.Span.ToArray());
        Assert.Equal(toRoom, move.ToRoom);
    }

    [Fact]
    public void Flee_NoFleeIntents_DoesNothing()
    {
        var system = CreateSystem(new FixedRandom());
        system.Tick(_f.State); // should not throw

        Assert.Empty(_fleeBlockedEvents);
        Assert.Empty(_f.Intents.Move.Span.ToArray());
    }
}
