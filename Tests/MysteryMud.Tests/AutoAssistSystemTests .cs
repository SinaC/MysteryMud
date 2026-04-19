using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class AutoAssistSystemTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly AutoAssistSystem _sut;

    public AutoAssistSystemTests()
    {
        _sut = new AutoAssistSystem(_f.RoomEnteredEvents);
    }

    public void Dispose() => _f.Dispose();

    [Fact]
    public void GroupMember_WithAutoAssist_AssistsWhenMemberAttacked()
    {
        var room = _f.Room("temple").Build();
        var group = _f.Group().Build();

        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).WithAutoAssist().Build();
        var bob = _f.Player("Bob").WithLocation(room).InGroup(group).WithAutoAssist().Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();

        _f.AddGroupMembers(group, alice, bob);

        // orc attacks alice
        alice.Add(new CombatState { Target = orc }); alice.Add<NewCombatantTag>();
        orc.Add(new CombatState { Target = alice }); orc.Add<NewCombatantTag>();

        _sut.TickCombatInitiated(_f.State);

        Assert.True(bob.Has<CombatState>());
        Assert.Equal(orc, bob.Get<CombatState>().Target);
    }

    [Fact]
    public void GroupMember_WithoutAutoAssist_DoesNotAssist()
    {
        var room = _f.Room("temple").Build();
        var group = _f.Group().Build();

        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).WithAutoAssist().Build();
        var bob = _f.Player("Bob").WithLocation(room).InGroup(group).Build(); // no AutoAssist
        var orc = _f.Npc("Orc").WithLocation(room).Build();

        _f.AddGroupMembers(group, alice, bob);
        alice.Add(new CombatState { Target = orc }); alice.Add<NewCombatantTag>();
        orc.Add(new CombatState { Target = alice }); orc.Add<NewCombatantTag>();

        _sut.TickCombatInitiated(_f.State);

        Assert.False(bob.Has<CombatState>());
    }

    [Fact]
    public void GroupMember_InDifferentRoom_DoesNotAssist()
    {
        var room1 = _f.Room("market").Build();
        var room2 = _f.Room("temple").Build();
        var group = _f.Group().Build();

        var alice = _f.Player("Alice").WithLocation(room1).InGroup(group).WithAutoAssist().Build();
        var bob = _f.Player("Bob").WithLocation(room2).InGroup(group).WithAutoAssist().Build();
        var orc = _f.Npc("Orc").WithLocation(room1).Build();

        _f.AddGroupMembers(group, alice, bob);
        alice.Add(new CombatState { Target = orc }); alice.Add<NewCombatantTag>();
        orc.Add(new CombatState { Target = alice }); orc.Add<NewCombatantTag>();

        _sut.TickCombatInitiated(_f.State);

        Assert.False(bob.Has<CombatState>());
    }

    [Fact]
    public void Charmie_AlwaysAssistsMaster()
    {
        var room = _f.Room("market").Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bear = _f.Npc("Bear").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();

        alice.Add(new Charmies { Entities = [bear] });
        bear.Add(new Charmed { Master = alice });

        alice.Add(new CombatState { Target = orc }); alice.Add<NewCombatantTag>();
        orc.Add(new CombatState { Target = alice }); orc.Add<NewCombatantTag>();

        _sut.TickCombatInitiated(_f.State);

        Assert.True(bear.Has<CombatState>());
        Assert.Equal(orc, bear.Get<CombatState>().Target);
    }

    [Fact]
    public void NpcGuard_AssistsPlayerOnRoomEntry()
    {
        var room = _f.Room("market").Build();
        var guard = _f.Npc("Guard").WithLocation(room)
                      .WithNpcAssist(AssistFlags.GuardPlayers).Build();
        var player = _f.Player().WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();

        // fight already in progress in the room
        player.Add(new CombatState { Target = orc });
        orc.Add(new CombatState { Target = player });

        // guard walks in
        _f.RoomEnteredEvents.Add(new RoomEnteredEvent { Entity = guard, ToRoom = room });

        _sut.TickMovement(_f.State);

        Assert.True(guard.Has<CombatState>());
        Assert.Equal(orc, guard.Get<CombatState>().Target);
    }
}
