using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Systems;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class ThreatDecaySystemTests : IDisposable
{
    private readonly MudTestFixture _f;
    private readonly ThreatDecaySystem _system;

    public ThreatDecaySystemTests()
    {
        _f = new MudTestFixture();
        _system = new ThreatDecaySystem(_f.World);
    }

    public void Dispose() => _f.Dispose();

    private Entity CreateNpc(string name = "Mob") =>
        _f.Npc(name)
            .WithTag<ActiveThreatTag>()
            .Build();

    private Entity CreateAttacker(string name = "Player") =>
        _f.Player(name).Build();

    private void SetThreat(Entity npc, Entity attacker, decimal value, long lastUpdateTick = 0)
    {
        ref var table = ref npc.Get<ThreatTable>();
        table.Entries[attacker] = value;
        table.LastUpdateTick = lastUpdateTick;
    }

    private void Tick(int times = 1)
    {
        var currentTick = _f.State.CurrentTick;
        for (int i = 0; i < times; i++)
        {
            var gameState = new GameState { CurrentTick = currentTick++, CurrentTimeMs = currentTick * 1000 };
            _system.Tick(gameState);
        }
    }

    // --- Decay convergence ---

    private static int TicksToDecayToZero(decimal startValue)
    {
        int ticks = 0;
        while (startValue > 0)
        {
            startValue = Math.Floor(startValue * 0.98m);
            ticks++;
        }
        return ticks;
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(1000)]
    public void Decay_ReachesZeroWithinExpectedTicks(decimal startValue)
    {
        var npc = CreateNpc();
        var attacker = CreateAttacker();
        SetThreat(npc, attacker, startValue);

        Tick(TicksToDecayToZero(startValue));

        Assert.False(npc.Has<ActiveThreatTag>());
        Assert.Empty(npc.Get<ThreatTable>().Entries);
    }

    [Fact]
    public void Decay_ValueNotYetZero_EntryAndTagRetained()
    {
        var npc = CreateNpc();
        var attacker = CreateAttacker();
        SetThreat(npc, attacker, 1000m);

        Tick(1); // one tick is always safe — 1000 * 0.98 = 980

        Assert.True(npc.Has<ActiveThreatTag>());
        Assert.True(npc.Get<ThreatTable>().Entries.ContainsKey(attacker));
    }

    // --- Floor behavior ---

    [Fact]
    public void Decay_FloorAppliedEachTick()
    {
        var npc = CreateNpc();
        var attacker = CreateAttacker();
        SetThreat(npc, attacker, 10m); // 10 * 0.98 = 9.8 -> floor -> 9

        Tick(1);

        Assert.Equal(9m, npc.Get<ThreatTable>().Entries[attacker]);
    }

    [Fact]
    public void Decay_Value1_RemovedAfterOneTick()
    {
        var npc = CreateNpc();
        var attacker = CreateAttacker();
        SetThreat(npc, attacker, 1m); // 1 * 0.98 = 0.98 -> floor -> 0 -> removed

        Tick(1);

        Assert.False(npc.Get<ThreatTable>().Entries.ContainsKey(attacker));
        Assert.False(npc.Has<ActiveThreatTag>());
    }

    // --- ActiveThreatTag management ---

    [Fact]
    public void Decay_WhenEntriesRemain_RetainsActiveTag()
    {
        var npc = CreateNpc();
        var attacker = CreateAttacker();
        SetThreat(npc, attacker, 100m);

        Tick(1);

        Assert.True(npc.Has<ActiveThreatTag>());
        Assert.True(npc.Get<ThreatTable>().Entries.ContainsKey(attacker));
    }

    // --- Multiple entries ---

    [Fact]
    public void Decay_MultipleEntries_LowThreatRemovedWhileHighThreatRemains()
    {
        var npc = CreateNpc();
        var lowAttacker = CreateAttacker("LowThreat");
        var highAttacker = CreateAttacker("HighThreat");
        SetThreat(npc, lowAttacker, 3m);   // gone by tick 3
        SetThreat(npc, highAttacker, 100m); // still alive at tick 3

        Tick(3);

        var entries = npc.Get<ThreatTable>().Entries;
        Assert.False(entries.ContainsKey(lowAttacker), "Low threat entry should be removed");
        Assert.True(entries.ContainsKey(highAttacker), "High threat entry should remain");
        Assert.True(npc.Has<ActiveThreatTag>(), "Tag should remain while any entry exists");
    }

    [Fact]
    public void Decay_MultipleEntries_TagRemovedWhenLastEntryDecays()
    {
        var npc = CreateNpc();
        var attackerA = CreateAttacker("A");
        var attackerB = CreateAttacker("B");
        SetThreat(npc, attackerA, 1m);
        SetThreat(npc, attackerB, 1m);

        Tick(1);

        Assert.False(npc.Has<ActiveThreatTag>());
        Assert.Empty(npc.Get<ThreatTable>().Entries);
    }

    // --- Timeout ---

    [Fact]
    public void Decay_WhenTimeoutExceeded_ClearsAllEntriesAndRemovesTag()
    {
        var npc = CreateNpc();
        var attacker = CreateAttacker();
        SetThreat(npc, attacker, 1000m, lastUpdateTick: 0);

        // jump past Timeout (100 ticks) in one go
        var newState = new GameState { CurrentTick = 101, CurrentTimeMs = 101 };
        _system.Tick(newState);

        Assert.Empty(npc.Get<ThreatTable>().Entries);
        Assert.False(npc.Has<ActiveThreatTag>());
    }

    [Fact]
    public void Decay_AtExactTimeout_DecaysNormallyWithoutClearing()
    {
        var npc = CreateNpc();
        var attacker = CreateAttacker();
        SetThreat(npc, attacker, 1000m, lastUpdateTick: 0);

        // tick 100: delta == Timeout, not strictly greater, so normal decay applies
        var newState = new GameState { CurrentTick = 100, CurrentTimeMs = 100 };
        _system.Tick(newState);

        Assert.True(npc.Get<ThreatTable>().Entries.ContainsKey(attacker),
            "Entry should still exist at exact timeout boundary");
    }
}
