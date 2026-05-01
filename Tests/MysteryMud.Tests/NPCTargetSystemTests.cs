using DefaultEcs;
using Microsoft.Extensions.Logging.Abstractions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Events;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class NPCTargetSystemTests : IDisposable
{
    private readonly MudTestFixture _f;
    private readonly TestEventBuffer<AggressedEvent> _aggressedEvents;
    private readonly NPCTargetSystem _system;

    public NPCTargetSystemTests()
    {
        _f = new MudTestFixture();
        _aggressedEvents = new TestEventBuffer<AggressedEvent>();
        _system = new NPCTargetSystem(_f.World, NullLogger.Instance, _aggressedEvents);
    }

    public void Dispose() => _f.Dispose();

    private Entity CreateRoom() => _f.Room().Build();

    private Entity CreateNpcInRoom(Entity room) =>
        _f.Npc()
            .WithTag<ActiveThreatTag>()
            .WithLocation(room)
            .Build();

    private Entity CreatePlayerInRoom(Entity room, string name = "Player") =>
        _f.Player(name)
            .WithLocation(room)
            .Build();

    private void SetThreat(Entity npc, Entity attacker, decimal value)
    {
        ref var table = ref npc.Get<ThreatTable>();
        table.Entries[attacker] = value;
    }

    // --- No valid targets ---

    [Fact]
    public void Targeting_NoEntriesInThreatTable_NoCombatStarted()
    {
        var room = CreateRoom();
        var npc = CreateNpcInRoom(room);

        _system.Tick(_f.State);

        Assert.False(npc.Has<CombatState>());
        Assert.Empty(_aggressedEvents);
    }

    [Fact]
    public void Targeting_AttackerInDifferentRoom_NoCombatStarted()
    {
        var npcRoom = CreateRoom();
        var otherRoom = CreateRoom();

        var npc = CreateNpcInRoom(npcRoom);
        var attacker = CreatePlayerInRoom(otherRoom);
        SetThreat(npc, attacker, 50m);

        _system.Tick(_f.State);

        Assert.False(npc.Has<CombatState>());
        Assert.Empty(_aggressedEvents);
    }

    [Fact]
    public void Targeting_AttackerHasNoLocation_NoCombatStarted()
    {
        var room = CreateRoom();
        var npc = CreateNpcInRoom(room);
        var attacker = _f.Player().Build(); // no location
        SetThreat(npc, attacker, 50m);

        _system.Tick(_f.State);

        Assert.False(npc.Has<CombatState>());
        Assert.Empty(_aggressedEvents);
    }

    // --- Aggression (no CombatState yet) ---

    [Fact]
    public void Targeting_NotInCombat_FiresAggressedEventForHighestThreatInRoom()
    {
        var room = CreateRoom();
        var npc = CreateNpcInRoom(room);
        var attacker = CreatePlayerInRoom(room);
        SetThreat(npc, attacker, 50m);

        _system.Tick(_f.State);

        Assert.False(npc.Has<CombatState>());
        Assert.Single(_aggressedEvents);
        Assert.Equal(npc, _aggressedEvents.Single().Source);
        Assert.Equal(attacker, _aggressedEvents.Single().Target);
    }

    [Fact]
    public void Targeting_NotInCombat_MultipleAttackers_AggressesHighestThreat()
    {
        var room = CreateRoom();
        var npc = CreateNpcInRoom(room);
        var lowThreat = CreatePlayerInRoom(room, "Low");
        var highThreat = CreatePlayerInRoom(room, "High");
        SetThreat(npc, lowThreat, 10m);
        SetThreat(npc, highThreat, 50m);

        _system.Tick(_f.State);

        Assert.Single(_aggressedEvents);
        Assert.Equal(highThreat, _aggressedEvents.Single().Target);
    }

    [Fact]
    public void Targeting_NotInCombat_HighestThreatInDifferentRoom_AggressesBestInSameRoom()
    {
        var npcRoom = CreateRoom();
        var otherRoom = CreateRoom();
        var npc = CreateNpcInRoom(npcRoom);

        var inRoom = CreatePlayerInRoom(npcRoom, "InRoom");
        var elsewhere = CreatePlayerInRoom(otherRoom, "Elsewhere");
        SetThreat(npc, inRoom, 30m);
        SetThreat(npc, elsewhere, 99m); // higher threat but wrong room

        _system.Tick(_f.State);

        Assert.Single(_aggressedEvents);
        Assert.Equal(inRoom, _aggressedEvents.Single().Target);
    }

    // --- Target switching (already in CombatState) ---

    [Fact]
    public void Targeting_InCombat_CurrentTargetIsStillHighest_TargetUnchanged()
    {
        var room = CreateRoom();
        var npc = CreateNpcInRoom(room);
        var current = CreatePlayerInRoom(room, "Current");
        var other = CreatePlayerInRoom(room, "Other");
        SetThreat(npc, current, 100m);
        SetThreat(npc, other, 10m);
        npc.Set(new CombatState { Target = current });

        _system.Tick(_f.State);

        Assert.Equal(current, npc.Get<CombatState>().Target);
        Assert.Empty(_aggressedEvents);
    }

    [Fact]
    public void Targeting_InCombat_NewAttackerHasHigherThreat_SwitchesTarget()
    {
        var room = CreateRoom();
        var npc = CreateNpcInRoom(room);
        var current = CreatePlayerInRoom(room, "Current");
        var newTop = CreatePlayerInRoom(room, "NewTop");
        SetThreat(npc, current, 30m);
        SetThreat(npc, newTop, 90m);
        npc.Set(new CombatState { Target = current });

        _system.Tick(_f.State);

        Assert.Equal(newTop, npc.Get<CombatState>().Target);
        Assert.Empty(_aggressedEvents); // switch, not a new aggression
    }

    [Fact]
    public void Targeting_InCombat_HighestThreatLeavesRoom_KeepsCurrentTarget()
    {
        var npcRoom = CreateRoom();
        var otherRoom = CreateRoom();
        var npc = CreateNpcInRoom(npcRoom);
        var current = CreatePlayerInRoom(npcRoom, "Current");
        var fled = CreatePlayerInRoom(otherRoom, "Fled"); // was top threat, now gone
        SetThreat(npc, current, 30m);
        SetThreat(npc, fled, 90m);
        npc.Set(new CombatState { Target = current });

        _system.Tick(_f.State);

        Assert.Equal(current, npc.Get<CombatState>().Target);
        Assert.Empty(_aggressedEvents);
    }

    [Fact]
    public void Targeting_InCombat_AllAttackersLeaveRoom_NoRetarget()
    {
        var npcRoom = CreateRoom();
        var otherRoom = CreateRoom();
        var npc = CreateNpcInRoom(npcRoom);
        var fled = CreatePlayerInRoom(otherRoom, "Fled");
        SetThreat(npc, fled, 50m);
        npc.Set(new CombatState { Target = fled });

        _system.Tick(_f.State);

        // target unchanged, no new aggression — CombatState cleanup is another system's job
        Assert.Equal(fled, npc.Get<CombatState>().Target);
        Assert.Empty(_aggressedEvents);
    }
}