using DefaultEcs;
using Microsoft.Extensions.Logging.Abstractions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Systems;
using MysteryMud.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.Tests;

public class NPCTargetSystemTests : IDisposable
{
    private readonly MudTestFixture _f;
    private readonly NPCTargetSystem _system;

    public NPCTargetSystemTests()
    {
        _f = new MudTestFixture();
        _system = new NPCTargetSystem(_f.World, NullLogger.Instance);
    }

    public void Dispose() => _f.Dispose();

    private Entity CreateNpcInCombat(Entity initialTarget)
    {
        var npc = _f.Npc()
            .WithTag<ActiveThreatTag>()
            .With(new CombatState { Target = initialTarget })
            .Build();
        return npc;
    }

    private void SetThreat(Entity npc, Entity attacker, decimal value)
    {
        ref var table = ref npc.Get<ThreatTable>();
        table.Entries[attacker] = value;
    }

    // --- Target selection ---

    [Fact]
    public void Targeting_SelectsHighestThreatAsTarget()
    {
        var lowThreat = _f.Player("Low").Build();
        var highThreat = _f.Player("High").Build();

        var npc = CreateNpcInCombat(lowThreat);
        SetThreat(npc, lowThreat, 10m);
        SetThreat(npc, highThreat, 50m);

        _system.Tick(_f.State);

        Assert.Equal(highThreat, npc.Get<CombatState>().Target);
    }

    [Fact]
    public void Targeting_CurrentTargetIsHighestThreat_TargetUnchanged()
    {
        var other = _f.Player("Other").Build();
        var topdog = _f.Player("TopDog").Build();

        var npc = CreateNpcInCombat(topdog);
        SetThreat(npc, other, 10m);
        SetThreat(npc, topdog, 100m);

        _system.Tick(_f.State);

        Assert.Equal(topdog, npc.Get<CombatState>().Target);
    }

    [Fact]
    public void Targeting_SingleEntry_TargetSetToThatEntry()
    {
        var attacker = _f.Player().Build();
        var npc = CreateNpcInCombat(attacker);
        SetThreat(npc, attacker, 50m);

        _system.Tick(_f.State);

        Assert.Equal(attacker, npc.Get<CombatState>().Target);
    }

    // --- No CombatState ---

    [Fact]
    public void Targeting_NpcWithNoActiveThreatTag_IsIgnored()
    {
        // NPC with threat table but no ActiveThreatTag — should not be processed
        var attacker = _f.Player().Build();
        var npc = _f.Npc().Build(); // no ActiveThreatTag
        ref var table = ref npc.Get<ThreatTable>();
        table.Entries[attacker] = 50m;

        // should not throw and NPC should have no CombatState
        _system.Tick(_f.State);

        Assert.False(npc.Has<CombatState>());
    }

    [Fact]
    public void Targeting_NpcHasThreatButNoCombatState_TargetNotForced()
    {
        // Active threat tag present but no CombatState — system should skip target switching
        var attacker = _f.Player().Build();
        var npc = _f.Npc()
            .WithTag<ActiveThreatTag>()
            .Build();
        SetThreat(npc, attacker, 50m);

        _system.Tick(_f.State);

        Assert.False(npc.Has<CombatState>());
    }

    // --- Target switching ---

    [Fact]
    public void Targeting_WhenNewAttackerExceedsCurrentTarget_SwitchesTarget()
    {
        var original = _f.Player("Original").Build();
        var newThreat = _f.Player("NewThreat").Build();

        var npc = CreateNpcInCombat(original);
        SetThreat(npc, original, 30m);
        SetThreat(npc, newThreat, 90m);

        _system.Tick(_f.State);

        Assert.Equal(newThreat, npc.Get<CombatState>().Target);
    }

    [Fact]
    public void Targeting_AfterDecay_HighestRemainingThreatBecomesTarget()
    {
        var decaySystem = new ThreatDecaySystem(_f.World);

        var fadeAway = _f.Player("FadeAway").Build();
        var persistent = _f.Player("Persistent").Build();

        var npc = CreateNpcInCombat(fadeAway);
        SetThreat(npc, fadeAway, 3m);   // gone after 3 ticks
        SetThreat(npc, persistent, 100m);

        // decay until fadeAway is gone
        var currentTick = _f.State.CurrentTick;
        for (int i = 1; i <= 3; i++)
        {
            var gameState = new GameState { CurrentTick = currentTick++, CurrentTimeMs = currentTick * 1000 };
            decaySystem.Tick(gameState);
        }
        _system.Tick(new GameState { CurrentTick = currentTick, CurrentTimeMs = currentTick * 1000 });

        Assert.Equal(persistent, npc.Get<CombatState>().Target);
    }
}
