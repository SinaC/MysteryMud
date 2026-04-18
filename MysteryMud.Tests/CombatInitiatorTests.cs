using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Intents;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class CombatInitiatorTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly AutoAssistSystem _autoAssist;
    private readonly DeathSystem _deathSystem;
    private readonly LootSystem _lootSystem;

    public CombatInitiatorTests()
    {
        // wire up systems with test doubles
        _autoAssist = new AutoAssistSystem(_f.RoomEnteredEvents);
        _deathSystem = new DeathSystem(_f.GameMessage, _f.Intents, _f.DeathEvents);
        _lootSystem = new LootSystem(_f.GameMessage, _f.Intents, _f.ItemLootedEvents);
    }

    public void Dispose() => _f.Dispose();

    // -------------------------------------------------------------------------
    // Basic initiator assignment
    // -------------------------------------------------------------------------

    [Fact]
    public void FirstAttacker_BecomesInitiator()
    {
        var room = _f.Room().Build();
        var npc = _f.Npc("Orc").WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();

        SetInitiator(npc, alice);

        Assert.True(npc.Has<CombatInitiator>());
        Assert.Equal(alice, npc.Get<CombatInitiator>().Claims[0].Claimant);
        Assert.False(npc.Get<CombatInitiator>().Claims[0].Forfeited);
    }

    [Fact]
    public void SecondAttacker_AddsClaimButDoesNotReplaceInitiator()
    {
        var room = _f.Room().Build();
        var npc = _f.Npc("Orc").WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room).Build();

        SetInitiator(npc, alice, tick: 1);
        AddClaim(npc, bob, tick: 5);

        ref var initiator = ref npc.Get<CombatInitiator>();
        Assert.Equal(2, initiator.Claims.Count);
        Assert.Equal(alice, initiator.Claims[0].Claimant); // alice still first
        Assert.Equal(bob, initiator.Claims[1].Claimant);
    }

    [Fact]
    public void DuplicateClaim_IsIgnored()
    {
        var room = _f.Room().Build();
        var npc = _f.Npc("Orc").WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();

        SetInitiator(npc, alice, tick: 1);
        AddClaim(npc, alice, tick: 5); // same player again

        Assert.Single(npc.Get<CombatInitiator>().Claims);
    }

    [Fact]
    public void Npc_DoesNotBecomeCombatClaimant()
    {
        var room = _f.Room().Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        var guard = _f.Npc("Guard").WithLocation(room).Build();

        SetInitiator(orc, guard, tick: 1); // NPC attacking NPC

        Assert.False(orc.Has<CombatInitiator>()); // no claim added
    }

    // -------------------------------------------------------------------------
    // Initiator forfeits — death
    // -------------------------------------------------------------------------

    [Fact]
    public void Initiator_ForfeitsClaim_WhenDead_AndSecondAttackerGetsLoot()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse("corpse", items: [sword]).WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).WithAutoLoot().Build();
        var bob = _f.Player("Bob").WithLocation(room).WithAutoLoot().Build();

        // alice initiated but died (forfeited), bob second
        corpse.Add(new CombatInitiator
        {
            Claims =
            [
                new CombatClaim { Claimant = alice, JoinedAtTick = 1, Forfeited = true }, // forfeited on death
                new CombatClaim { Claimant = bob,   JoinedAtTick = 5, Forfeited = false },
            ]
        });

        _f.Intents.CorpseLoot.Add() = new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = bob,
            LootOwnerGroup = Entity.Null
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, bob.Get<Inventory>().Items);
        Assert.DoesNotContain(sword, alice.Get<Inventory>().Items);
    }

    [Fact]
    public void Initiator_ForfeitsClaim_WhenDead_AndNoOtherClaimants_KillerGetsLoot()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse("corpse", items: [sword]).WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).WithAutoLoot().Build();
        var bob = _f.Player("Bob").WithLocation(room).WithAutoLoot().Build();

        // alice initiated but died, no other claimants
        corpse.Add(new CombatInitiator
        {
            Claims =
            [
                new CombatClaim { Claimant = alice, JoinedAtTick = 1, Forfeited = true },
            ]
        });

        _f.Intents.CorpseLoot.Add() = new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = bob,
            LootOwnerGroup = Entity.Null
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, bob.Get<Inventory>().Items); // falls back to killer
    }

    // -------------------------------------------------------------------------
    // Initiator forfeits — flee
    // -------------------------------------------------------------------------

    [Fact]
    public void Initiator_ForfeitsClaim_WhenFleeing()
    {
        var room = _f.Room().Build();
        var npc = _f.Npc("Orc").WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room).Build();

        SetInitiator(npc, alice, tick: 1);
        AddClaim(npc, bob, tick: 5);

        // alice flees — forfeit her claim
        ForfeitClaim(npc, alice);

        ref var initiator = ref npc.Get<CombatInitiator>();
        Assert.True(initiator.Claims[0].Forfeited);   // alice forfeited
        Assert.False(initiator.Claims[1].Forfeited);  // bob still valid
    }

    [Fact]
    public void AfterInitiatorFlees_SecondAttacker_GetsLoot()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse("corpse", items: [sword]).WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).WithAutoLoot().Build();
        var bob = _f.Player("Bob").WithLocation(room).WithAutoLoot().Build();

        corpse.Add(new CombatInitiator
        {
            Claims =
            [
                new CombatClaim { Claimant = alice, JoinedAtTick = 1, Forfeited = true }, // fled
                new CombatClaim { Claimant = bob,   JoinedAtTick = 5, Forfeited = false },
            ]
        });

        _f.Intents.CorpseLoot.Add() = new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = bob,
            LootOwnerGroup = Entity.Null
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, bob.Get<Inventory>().Items);
    }

    // -------------------------------------------------------------------------
    // Peace command
    // -------------------------------------------------------------------------

    [Fact]
    public void PeaceCommand_RemovesCombatInitiatorFromAllInRoom()
    {
        var room = _f.Room().Build();
        var npc = _f.Npc("Orc").WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room).Build();

        SetInitiator(npc, alice, tick: 1);
        AddClaim(npc, bob, tick: 5);

        PeaceRoom(room);

        Assert.False(npc.Has<CombatInitiator>());
    }

    [Fact]
    public void AfterPeace_NewAttacker_GetsLoot()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse("corpse", items: [sword]).WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).WithAutoLoot().Build();
        var charlie = _f.Player("Charlie").WithLocation(room).WithAutoLoot().Build();

        SetInitiator(corpse, alice, tick: 1);
        PeaceRoom(room); // wipes CombatInitiator entirely

        // charlie attacks after peace — becomes new initiator
        SetInitiator(corpse, charlie, tick: 10);

        _f.Intents.CorpseLoot.Add() = new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = charlie,
            LootOwnerGroup = Entity.Null
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, charlie.Get<Inventory>().Items);
    }

    // -------------------------------------------------------------------------
    // The full scenario from the design discussion
    // A starts fight, B joins, A dies, C joins, B kills
    // -------------------------------------------------------------------------

    [Fact]
    public void FullScenario_A_Dies_C_Joins_B_Kills_B_GetsLoot()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse("corpse", items: [sword]).WithLocation(room).Build();
        var alice = _f.Player("Alice").WithLocation(room).WithAutoLoot().Build();
        var bob = _f.Player("Bob").WithLocation(room).WithAutoLoot().Build();
        var charlie = _f.Player("Charlie").WithLocation(room).WithAutoLoot().Build();

        // tick 1: A starts fight
        // tick 5: B joins
        // tick 10: NPC kills A -> alice forfeited
        // tick 12: C joins
        // tick 15: B kills NPC
        corpse.Add(new CombatInitiator
        {
            Claims =
            [
                new CombatClaim { Claimant = alice,   JoinedAtTick = 1,  Forfeited = true },  // died
                new CombatClaim { Claimant = bob,     JoinedAtTick = 5,  Forfeited = false },
                new CombatClaim { Claimant = charlie, JoinedAtTick = 12, Forfeited = false },
            ]
        });

        _f.Intents.CorpseLoot.Add() = new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = bob,
            LootOwnerGroup = Entity.Null
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, bob.Get<Inventory>().Items);     // bob gets it — first non-forfeited
        Assert.DoesNotContain(sword, alice.Get<Inventory>().Items);
        Assert.DoesNotContain(sword, charlie.Get<Inventory>().Items);
    }

    // -------------------------------------------------------------------------
    // No claims at all — killer gets loot
    // -------------------------------------------------------------------------

    [Fact]
    public void NoCombatInitiator_KillerGetsLoot()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var corpse = _f.Corpse("corpse", items: [sword]).WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room).WithAutoLoot().Build();

        // no CombatInitiator component at all
        _f.Intents.CorpseLoot.Add() = new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = bob,
            LootOwnerGroup = Entity.Null
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, bob.Get<Inventory>().Items);
    }

    // -------------------------------------------------------------------------
    // Helpers — mirror what the real systems would do
    // -------------------------------------------------------------------------

    private static void SetInitiator(Entity npc, Entity claimant, int tick = 0)
    {
        CharacterHelpers.AddCombatClaim(npc, claimant, tick);
    }

    private static void AddClaim(Entity npc, Entity claimant, int tick)
        => SetInitiator(npc, claimant, tick); // same logic, named for readability

    private static void ForfeitClaim(Entity npc, Entity claimant)
    {
        CharacterHelpers.ForfeitClaim(npc, claimant);
    }

    private static void PeaceRoom(Entity room)
    {
        ref var contents = ref room.Get<RoomContents>();
        foreach (var character in contents.Characters)
        {
            character.Remove<CombatState>();
            character.Remove<NewCombatantTag>();
            character.Remove<CombatInitiator>();
            character.Remove<ActiveThreatTag>();
            ref var threatTable = ref character.TryGetRef<ThreatTable>(out var hasThreatTable);
            if (hasThreatTable)
            {
                threatTable.Threat.Clear();
                threatTable.LastUpdateTick = 0;
            }
        }
    }
}
