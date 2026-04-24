using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Persistence;
using MysteryMud.Tests.Infrastructure;
using TinyECS;

namespace MysteryMud.Tests;

public class DeathSystemTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly DeathSystem _sut;
    private readonly TestFollowService _followService = new();

    public DeathSystemTests()
    {
        var dirtyTracker = new DirtyTracker();
        _sut = new DeathSystem(
            _f.World,
            _followService,
            dirtyTracker,
            _f.Intents,
            _f.DeathEvents);
    }

    // -------------------------------------------------------------------------
    // Casting
    // -------------------------------------------------------------------------

    [Fact]
    public void Victim_CastingRemoved_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        _f.World.Add<Casting>(alice);

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = EntityId.Invalid };
        _sut.Tick(_f.State);

        Assert.False(_f.World.Has<Casting>(alice));
    }

    // -------------------------------------------------------------------------
    // Follow
    // -------------------------------------------------------------------------

    [Fact]
    public void Victim_StopsFollowing_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room)
                      .With(new Following { Leader = alice })
                      .Build();
        _f.World.Add(alice, new Followers { Entities = [bob] });

        _f.DeathEvents.Add() = new DeathEvent { Victim = bob, Killer = EntityId.Invalid };
        _sut.Tick(_f.State);

        Assert.Contains(bob, _followService.StopFollowingCalled);
    }

    [Fact]
    public void Victim_AllFollowersStopped_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room)
                      .With(new Following { Leader = alice })
                      .Build();
        var carol = _f.Player("Carol").WithLocation(room)
                      .With(new Following { Leader = alice })
                      .Build();
        _f.World.Add(alice, new Followers { Entities = [bob, carol] });

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = EntityId.Invalid };
        _sut.Tick(_f.State);

        Assert.Contains(alice, _followService.StopAllFollowersCalled);
    }

    // -------------------------------------------------------------------------
    // Combat
    // -------------------------------------------------------------------------

    [Fact]
    public void Victim_RemovedFromCombat_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        _f.World.Add(alice, new CombatState { Target = orc });
        _f.World.Add(orc, new CombatState { Target = alice });

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc };
        _sut.Tick(_f.State);

        Assert.False(_f.World.Has<CombatState>(alice));
        Assert.False(_f.World.Has<CombatState>(orc)); // orc no longer targeting dead alice
    }

    [Fact]
    public void Victim_RemovedFromAllThreatTables_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 100 } })
                      .Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 50 } })
                      .Build();
        _f.World.Add<ActiveThreatTag>(orc1);
        _f.World.Add<ActiveThreatTag>(orc2);

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc1 };
        _sut.Tick(_f.State);

        Assert.False(_f.World.Get<ThreatTable>(orc1).Threat.ContainsKey(alice));
        Assert.False(_f.World.Get<ThreatTable>(orc2).Threat.ContainsKey(alice));
    }

    [Fact]
    public void Victim_OwnThreatTable_Cleared_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 100 } })
                      .Build();
        _f.World.Add<ActiveThreatTag>(orc);

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = alice };
        _sut.Tick(_f.State);

        // orc's own threat table cleared by RemoveFromCombat
        Assert.Empty(_f.World.Get<ThreatTable>(orc).Threat);
        Assert.False(_f.World.Has<ActiveThreatTag>(orc));
    }

    [Fact]
    public void Victim_RemovedFromOtherNpcs_ThreatTables_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 100 } })
                      .Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<EntityId, long> { [alice] = 50 } })
                      .Build();
        _f.World.Add<ActiveThreatTag>(orc1);
        _f.World.Add<ActiveThreatTag>(orc2);

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc1 };
        _sut.Tick(_f.State);

        // alice removed from other NPCs' threat tables by RemoveFromAllThreatTable
        Assert.False(_f.World.Get<ThreatTable>(orc1).Threat.ContainsKey(alice));
        Assert.False(_f.World.Get<ThreatTable>(orc2).Threat.ContainsKey(alice));
    }

    [Fact]
    public void Victim_CombatInitiator_Removed_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        _f.World.Add(orc, new CombatInitiator { Claims = [] }); // orc had initiator component
        _f.World.Add(orc, new CombatState { Target = alice });

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = alice };
        _sut.Tick(_f.State);

        // RemoveFromCombat removes CombatInitiator
        Assert.False(_f.World.Has<CombatInitiator>(orc));
    }

    [Fact]
    public void Victim_NewCombatantTag_Removed_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        _f.World.Add(alice,new CombatState { Target = orc });
        _f.World.Add<NewCombatantTag>(alice);

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc };
        _sut.Tick(_f.State);

        Assert.False(_f.World.Has<NewCombatantTag>(alice));
        Assert.False(_f.World.Has<CombatState>(alice));
    }

    // -------------------------------------------------------------------------
    // Combat claims
    // -------------------------------------------------------------------------

    [Fact]
    public void PlayerVictim_AllCombatClaims_Forfeited_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room).Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room).Build();
        CombatHelpers.AddCombatClaim(_f.World, _f.State, orc1, alice);
        CombatHelpers.AddCombatClaim(_f.World, _f.State, orc2, alice);

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc1 };
        _sut.Tick(_f.State);

        Assert.True(_f.World.Get<CombatInitiator>(orc1).Claims[0].Forfeited);
        Assert.True(_f.World.Get<CombatInitiator>(orc2).Claims[0].Forfeited);
    }

    [Fact]
    public void NpcVictim_AliceClaim_NotForfeited_AliceGetsLoot()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        CombatHelpers.AddCombatClaim(_f.World, _f.State, orc, alice);

        // orc dies — killed by someone else
        var bob = _f.Player("Bob").WithLocation(room).Build();
        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = bob };
        _sut.Tick(_f.State);

        // alice initiated so she gets loot despite bob delivering kill
        var lootIntent = _f.Intents.CorpseLoot.Span[0];
        Assert.Equal(alice, lootIntent.LootOwner);
    }

    [Fact]
    public void Victim_NullKiller_NoLootIntent_WhenNoInitiator()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();

        // environmental death — no killer, no initiator
        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = EntityId.Invalid };
        _sut.Tick(_f.State);

        Assert.Equal(0, _f.Intents.CorpseLoot.Count);
    }

    [Fact]
    public void Victim_NullKiller_InitiatorStillGetsLoot()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        CombatHelpers.AddCombatClaim(_f.World, _f.State, orc, alice);

        // orc dies environmentally — no killer but alice had initiated
        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = EntityId.Invalid };
        _sut.Tick(_f.State);

        Assert.Equal(1, _f.Intents.CorpseLoot.Count);
        Assert.Equal(alice, _f.Intents.CorpseLoot.Span[0].LootOwner);
    }

    // -------------------------------------------------------------------------
    // Corpse creation
    // -------------------------------------------------------------------------

    [Fact]
    public void Victim_CorpseCreated_InSameRoom()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                      .With(new Inventory { Items = [sword] })
                      .Build();
        _f.World.Add(sword, new ContainedIn { Character = orc });
        var bob = _f.Player("Bob").WithLocation(room).Build();

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = bob }; // killer must be a player to generate a loot intent
        _sut.Tick(_f.State);

        Assert.True(_f.Intents.CorpseLoot.Count > 0);
        var lootIntent = _f.Intents.CorpseLoot.Span[0];
        Assert.True(_f.World.IsAlive(lootIntent.Corpse));
        Assert.True(_f.World.Has<ContainerContents>(lootIntent.Corpse));
    }

    [Fact]
    public void Victim_Items_MovedToCorpse_OnDeath()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                       .With(new Inventory { Items = [sword] })
                       .Build();
        _f.World.Add(sword, new ContainedIn { Character = orc });
        var bob = _f.Player("Bob").WithLocation(room).Build();

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = bob }; // killer must be a player to generate a loot intent
        _sut.Tick(_f.State);

        var corpse = _f.Intents.CorpseLoot.Span[0].Corpse;
        Assert.Contains(sword, _f.World.Get<ContainerContents>(corpse).Items);
        Assert.Empty(_f.World.Get<Inventory>(bob).Items);
    }

    [Fact]
    public void Victim_WithoutLocation_NoCorpseCreated()
    {
        var alice = _f.Player("Alice").Build(); // no location
        var orc = _f.Npc("Orc").Build();

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc };
        _sut.Tick(_f.State);

        Assert.Equal(0, _f.Intents.CorpseLoot.Count);
    }

    // -------------------------------------------------------------------------
    // Loot ownership
    // -------------------------------------------------------------------------

    [Fact]
    public void Killer_GetsLootOwnership_WhenNoInitiator()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = alice };
        _sut.Tick(_f.State);

        var lootIntent = _f.Intents.CorpseLoot.Span[0];
        Assert.Equal(alice, lootIntent.LootOwner);
    }

    [Fact]
    public void Initiator_GetsLootOwnership_EvenIfNotKiller()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        CombatHelpers.AddCombatClaim(_f.World, new GameState { CurrentTick = 1, CurrentTimeMs = 1 }, orc, alice);
        CombatHelpers.AddCombatClaim(_f.World, new GameState { CurrentTick = 5, CurrentTimeMs = 5 }, orc, bob);

        // bob delivers killing blow
        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = bob };
        _sut.Tick(_f.State);

        var lootIntent = _f.Intents.CorpseLoot.Span[0];
        Assert.Equal(alice, lootIntent.LootOwner); // alice was first initiator
    }

    [Fact]
    public void DeadInitiator_KillerGetsLootOwnership()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();

        CombatHelpers.AddCombatClaim(_f.World, _f.State, orc, alice);
        // alice died — forfeit her claim
        CombatHelpers.ForfeitClaim(_f.World, orc, alice);

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = bob };
        _sut.Tick(_f.State);

        var lootIntent = _f.Intents.CorpseLoot.Span[0];
        Assert.Equal(bob, lootIntent.LootOwner);
    }

    [Fact]
    public void LootOwnerGroup_SetFromOwnerGroupMembership()
    {
        var room = _f.Room().Build();
        var group = _f.Group().Build();
        var alice = _f.Player("Alice").WithLocation(room).InGroup(group).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        _f.AddGroupMembers(group, alice);
        CombatHelpers.AddCombatClaim(_f.World, _f.State, orc, alice);

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = alice };
        _sut.Tick(_f.State);

        var lootIntent = _f.Intents.CorpseLoot.Span[0];
        Assert.Equal(group, lootIntent.LootOwnerGroup);
    }

    public void Dispose() => _f.Dispose();

    // -------------------------------------------------------------------------
    // RemoveFromAllCombat
    // -------------------------------------------------------------------------
    [Fact]
    public void RemoveFromAllCombat_DoesNotWipe_OtherActors_ThreatTables()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var bob = _f.Player("Bob").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                      .With(new ThreatTable
                      {
                          Threat = new Dictionary<EntityId, long>
                          {
                              [alice] = 100,
                              [bob] = 50   // orc also has threat on bob
                          }
                      })
                      .Build();
        _f.World.Add<ActiveThreatTag>(orc);
        _f.World.Add(orc, new CombatState { Target = alice });

        CombatHelpers.RemoveFromAllCombat(_f.World, _f.State, alice);

        // orc's threat on bob must survive — only alice's entry should be removed separately
        Assert.False(_f.World.Has<CombatState>(orc));
        Assert.True(_f.World.Get<ThreatTable>(orc).Threat.ContainsKey(bob));  // bob's threat intact
        Assert.Equal(50, _f.World.Get<ThreatTable>(orc).Threat[bob]);
    }
}

// -------------------------------------------------------------------------
// Test doubles
// -------------------------------------------------------------------------

internal class TestFollowService : IFollowService
{
    public HashSet<EntityId> StopFollowingCalled { get; } = [];
    public HashSet<EntityId> StopAllFollowersCalled { get; } = [];

    public void Follow(EntityId follower, EntityId leader) { }

    public void StopFollowing(EntityId follower)
        => StopFollowingCalled.Add(follower);

    public void StopAllFollowers(EntityId leader)
        => StopAllFollowersCalled.Add(leader);
}
