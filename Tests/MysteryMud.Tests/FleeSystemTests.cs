using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.Tests;

public class FleeSystemTests : IDisposable
{
    private readonly MudTestFixture _fixture;
    private readonly TestEventBuffer<FleeBlockedEvent> _fleeBlockedEvents = new();

    public FleeSystemTests()
    {
        _fixture = new MudTestFixture();
    }

    public void Dispose() => _fixture.Dispose();

    private FleeSystem CreateSystem(IRandom random)
        => new(random, _fixture.GameMessage, _fixture.Intents, _fixture.TestExperienceService, _fleeBlockedEvents);

    // Helpers

    private (Entity player, Entity room) CreatePlayerInRoom()
    {
        var room = _fixture.Room().Build();
        var player = _fixture.Player()
            .WithLocation(room)
            .Build();
        return (player, room);
    }

    private Entity CreateConnectedRoom(Entity fromRoom, DirectionKind direction)
    {
        var toRoom = _fixture.Room().Build();
        ref var graph = ref fromRoom.Get<RoomGraph>();
        graph.Exits[direction] = new Exit { Direction = direction, TargetRoom = toRoom };
        return toRoom;
    }

    private void PutInCombat(Entity attacker, Entity target)
    {
        attacker.Add(new CombatState { Target = target });
        target.Add(new CombatState { Target = attacker });
    }

    private void QueueFlee(Entity entity, Entity fromRoom)
    {
        ref var flee = ref _fixture.Intents.Flee.Add();
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
        system.Tick(_fixture.State);

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
        system.Tick(_fixture.State);

        Assert.Empty(_fixture.Intents.Move.Span.ToArray());
    }

    [Fact]
    public void Flee_NoExits_EmitsNoExitBlockedEvent()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var system = CreateSystem(new FixedRandom());
        system.Tick(_fixture.State);

        var evt = Assert.Single(_fleeBlockedEvents);
        Assert.Equal(FleeBlockedReason.NoExit, evt.Reason);
    }

    [Fact]
    public void Flee_NoExits_SendsPanicMessage()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var system = CreateSystem(new FixedRandom());
        system.Tick(_fixture.State);

        Assert.Contains(_fixture.GameMessage.GetMessagesFor(player), m => m.Contains("PANIC"));
    }

    [Fact]
    public void Flee_AllAttemptsPickMissingDirection_EmitsFailedToFlee()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        // Only North exit exists, but all 6 picks are South
        CreateConnectedRoom(room, DirectionKind.North);
        var southPicks = Enumerable.Repeat(DirectionValue(DirectionKind.South), 6).ToArray();

        var system = CreateSystem(new FixedRandom(southPicks));
        system.Tick(_fixture.State);

        var evt = Assert.Single(_fleeBlockedEvents);
        Assert.Equal(FleeBlockedReason.FailedToFlee, evt.Reason);
    }

    [Fact]
    public void Flee_Success_EmitsMoveIntent()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var toRoom = CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_fixture.State);

        var move = Assert.Single(_fixture.Intents.Move.Span.ToArray());
        Assert.Equal(player, move.Actor);
        Assert.Equal(room, move.FromRoom);
        Assert.Equal(toRoom, move.ToRoom);
        Assert.True(move.AutoLook);
    }

    [Fact]
    public void Flee_Success_RemovesPlayerFromCombat()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_fixture.State);

        Assert.False(player.Has<CombatState>());
    }

    [Fact]
    public void Flee_Success_GrantsNegativeExperience()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_fixture.State);

        Assert.Equal(-10, _fixture.TestExperienceService.LastGranted);
    }

    [Fact]
    public void Flee_Success_SendsFleeMessages()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        CreateConnectedRoom(room, DirectionKind.North);
        var system = CreateSystem(new FixedRandom(DirectionValue(DirectionKind.North)));
        system.Tick(_fixture.State);

        Assert.Contains(_fixture.GameMessage.GetMessagesFor(player), m => m.Contains("flee"));
        Assert.Contains(_fixture.GameMessage.GetMessagesFor(npc), m => m.Contains("fled"));
    }

    [Fact]
    public void Flee_Success_FirstAttemptMisses_SecondSucceeds()
    {
        var (player, room) = CreatePlayerInRoom();
        var npc = _fixture.Npc().WithLocation(room).Build();
        PutInCombat(player, npc);
        QueueFlee(player, room);

        var toRoom = CreateConnectedRoom(room, DirectionKind.North);
        // First pick: South (no exit), second pick: North (valid)
        var system = CreateSystem(new FixedRandom(
            DirectionValue(DirectionKind.South),
            DirectionValue(DirectionKind.North)));
        system.Tick(_fixture.State);

        var move = Assert.Single(_fixture.Intents.Move.Span.ToArray());
        Assert.Equal(toRoom, move.ToRoom);
    }

    [Fact]
    public void Flee_NoFleeIntents_DoesNothing()
    {
        var system = CreateSystem(new FixedRandom());
        system.Tick(_fixture.State); // should not throw

        Assert.Empty(_fleeBlockedEvents);
        Assert.Empty(_fixture.Intents.Move.Span.ToArray());
    }
}
