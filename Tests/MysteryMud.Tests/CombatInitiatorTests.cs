using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Intents;
using MysteryMud.Tests.Infrastructure;
using TinyECS;

namespace MysteryMud.Tests;

public class CombatInitiatorTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly LootSystem _lootSystem;

    public CombatInitiatorTests()
    {
        // wire up systems with test doubles
        _lootSystem = new LootSystem(_f.World, _f.GameMessage, _f.Intents, _f.ItemLootedEvents);
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

        Assert.True(_f.World.Has<CombatInitiator>(npc));
        Assert.Equal(alice, _f.World.Get<CombatInitiator>(npc).Claims[0].Claimant);
        Assert.False(_f.World.Get<CombatInitiator>(npc).Claims[0].Forfeited);
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

        ref var initiator = ref _f.World.Get<CombatInitiator>(npc);
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

        Assert.Single(_f.World.Get<CombatInitiator>(npc).Claims);
    }

    [Fact]
    public void Npc_DoesNotBecomeCombatClaimant()
    {
        var room = _f.Room().Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        var guard = _f.Npc("Guard").WithLocation(room).Build();

        SetInitiator(orc, guard, tick: 1); // NPC attacking NPC

        Assert.False(_f.World.Has<CombatInitiator>(orc)); // no claim added
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
        _f.World.Add(corpse, new CombatInitiator
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
            LootOwnerGroup = EntityId.Invalid
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, _f.World.Get<Inventory>(bob).Items);
        Assert.DoesNotContain(sword, _f.World.Get<Inventory>(alice).Items);
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
        _f.World.Add(corpse, new CombatInitiator
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
            LootOwnerGroup = EntityId.Invalid
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, _f.World.Get<Inventory>(bob).Items); // falls back to killer
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

        ref var initiator = ref _f.World.Get<CombatInitiator>(npc);
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

        _f.World.Add(corpse,  new CombatInitiator
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
            LootOwnerGroup = EntityId.Invalid
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, _f.World.Get<Inventory>(bob).Items);
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

        PeaceRoom(_f.State, room);

        Assert.False(_f.World.Has<CombatInitiator>(npc));
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
        PeaceRoom(_f.State, room); // wipes CombatInitiator entirely

        // charlie attacks after peace — becomes new initiator
        SetInitiator(corpse, charlie, tick: 10);

        _f.Intents.CorpseLoot.Add() = new CorpseLootIntent
        {
            Corpse = corpse,
            LootOwner = charlie,
            LootOwnerGroup = EntityId.Invalid
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, _f.World.Get<Inventory>(charlie).Items);
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
        _f.World.Add(corpse, new CombatInitiator
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
            LootOwnerGroup = EntityId.Invalid
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, _f.World.Get<Inventory>(bob).Items);     // bob gets it — first non-forfeited
        Assert.DoesNotContain(sword, _f.World.Get<Inventory>(alice).Items);
        Assert.DoesNotContain(sword, _f.World.Get<Inventory>(charlie).Items);
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
            LootOwnerGroup = EntityId.Invalid
        };

        _lootSystem.Tick(_f.State);

        Assert.Contains(sword, _f.World.Get<Inventory>(bob).Items);
    }

    // -------------------------------------------------------------------------
    // Helpers — mirror what the real systems would do
    // -------------------------------------------------------------------------

    private void SetInitiator(EntityId npc, EntityId claimant, int tick = 0)
    {
        CombatHelpers.AddCombatClaim(_f.World, _f.State, npc, claimant);
    }

    private void AddClaim(EntityId npc, EntityId claimant, int tick)
        => SetInitiator(npc, claimant, tick); // same logic, named for readability

    private void ForfeitClaim(EntityId npc, EntityId claimant)
    {
        CombatHelpers.ForfeitClaim(_f.World, npc, claimant);
    }

    private void PeaceRoom(GameState state, EntityId room)
    {
        ref var contents = ref _f.World.Get<RoomContents>(room);
        foreach (var character in contents.Characters)
        {
            CombatHelpers.RemoveFromCombat(_f.World, state, character);
        }
    }
}
