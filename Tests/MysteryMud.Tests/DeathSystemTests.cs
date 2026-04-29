using DefaultEcs;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Persistence;
using MysteryMud.Tests.Infrastructure;

namespace MysteryMud.Tests;

public class DeathSystemTests : IDisposable
{
    private readonly MudTestFixture _f = new();
    private readonly DeathSystem _sut;
    private readonly TestFollowService _followService = new();
    private readonly CombatService _combatService;

    public DeathSystemTests()
    {
        _combatService = new CombatService(_f.World);
        var dirtyTracker = new DirtyTracker();
        _sut = new DeathSystem(
            _followService,
            _combatService,
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
        alice.Set<Casting>();

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = default };
        _sut.Tick(_f.State);

        Assert.False(alice.Has<Casting>());
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
        alice.Set(new Followers { Entities = [bob] });

        _f.DeathEvents.Add() = new DeathEvent { Victim = bob, Killer = default };
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
        alice.Set(new Followers { Entities = [bob, carol] });

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = default };
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
        alice.Set(new CombatState { Target = orc });
        orc.Set(new CombatState { Target = alice });

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc };
        _sut.Tick(_f.State);

        Assert.False(alice.Has<CombatState>());
        Assert.False(orc.Has<CombatState>()); // orc no longer targeting dead alice
    }

    [Fact]
    public void Victim_RemovedFromAllThreatTables_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<Entity, long> { [alice] = 100 } })
                      .Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<Entity, long> { [alice] = 50 } })
                      .Build();
        orc1.Set<ActiveThreatTag>();
        orc2.Set<ActiveThreatTag>();

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc1 };
        _sut.Tick(_f.State);

        Assert.False(orc1.Get<ThreatTable>().Threat.ContainsKey(alice));
        Assert.False(orc2.Get<ThreatTable>().Threat.ContainsKey(alice));
    }

    [Fact]
    public void Victim_OwnThreatTable_Cleared_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<Entity, long> { [alice] = 100 } })
                      .Build();
        orc.Set<ActiveThreatTag>();

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = alice };
        _sut.Tick(_f.State);

        // orc's own threat table cleared by RemoveFromCombat
        Assert.Empty(orc.Get<ThreatTable>().Threat);
        Assert.False(orc.Has<ActiveThreatTag>());
    }

    [Fact]
    public void Victim_RemovedFromOtherNpcs_ThreatTables_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc1 = _f.Npc("Orc1").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<Entity, long> { [alice] = 100 } })
                      .Build();
        var orc2 = _f.Npc("Orc2").WithLocation(room)
                      .With(new ThreatTable { Threat = new Dictionary<Entity, long> { [alice] = 50 } })
                      .Build();
        orc1.Set<ActiveThreatTag>();
        orc2.Set<ActiveThreatTag>();

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc1 };
        _sut.Tick(_f.State);

        // alice removed from other NPCs' threat tables by RemoveFromAllThreatTable
        Assert.False(orc1.Get<ThreatTable>().Threat.ContainsKey(alice));
        Assert.False(orc2.Get<ThreatTable>().Threat.ContainsKey(alice));
    }

    [Fact]
    public void Victim_CombatInitiator_Removed_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        orc.Set(new CombatInitiator { Claims = [] }); // orc had initiator component
        orc.Set(new CombatState { Target = alice });

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = alice };
        _sut.Tick(_f.State);

        // RemoveFromCombat removes CombatInitiator
        Assert.False(orc.Has<CombatInitiator>());
    }

    [Fact]
    public void Victim_NewCombatantTag_Removed_OnDeath()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        alice.Set(new CombatState { Target = orc });
        alice.Set<NewCombatantTag>();

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc };
        _sut.Tick(_f.State);

        Assert.False(alice.Has<NewCombatantTag>());
        Assert.False(alice.Has<CombatState>());
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
        _combatService.AddCombatClaim(orc1, alice, _f.State.CurrentTick);
        _combatService.AddCombatClaim(orc2, alice, _f.State.CurrentTick);

        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = orc1 };
        _sut.Tick(_f.State);

        Assert.True(orc1.Get<CombatInitiator>().Claims[0].Forfeited);
        Assert.True(orc2.Get<CombatInitiator>().Claims[0].Forfeited);
    }

    [Fact]
    public void NpcVictim_AliceClaim_NotForfeited_AliceGetsLoot()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        _combatService.AddCombatClaim(orc, alice, _f.State.CurrentTick);

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
        _f.DeathEvents.Add() = new DeathEvent { Victim = alice, Killer = default };
        _sut.Tick(_f.State);

        Assert.Equal(0, _f.Intents.CorpseLoot.Count);
    }

    [Fact]
    public void Victim_NullKiller_InitiatorStillGetsLoot()
    {
        var room = _f.Room().Build();
        var alice = _f.Player("Alice").WithLocation(room).Build();
        var orc = _f.Npc("Orc").WithLocation(room).Build();
        _combatService.AddCombatClaim(orc, alice, _f.State.CurrentTick);

        // orc dies environmentally — no killer but alice had initiated
        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = default };
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
        sword.Set(new ContainedIn { Character = orc });
        var bob = _f.Player("Bob").WithLocation(room).Build();

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = bob }; // killer must be a player to generate a loot intent
        _sut.Tick(_f.State);

        Assert.True(_f.Intents.CorpseLoot.Count > 0);
        var lootIntent = _f.Intents.CorpseLoot.Span[0];
        Assert.True(lootIntent.Corpse.IsAlive);
        Assert.True(lootIntent.Corpse.Has<ContainerContents>());
    }

    [Fact]
    public void Victim_Items_MovedToCorpse_OnDeath()
    {
        var room = _f.Room().Build();
        var sword = _f.Item("sword").Build();
        var orc = _f.Npc("Orc").WithLocation(room)
                       .With(new Inventory { Items = [sword] })
                       .Build();
        sword.Set(new ContainedIn { Character = orc });
        var bob = _f.Player("Bob").WithLocation(room).Build();

        _f.DeathEvents.Add() = new DeathEvent { Victim = orc, Killer = bob }; // killer must be a player to generate a loot intent
        _sut.Tick(_f.State);

        var corpse = _f.Intents.CorpseLoot.Span[0].Corpse;
        Assert.Contains(sword, corpse.Get<ContainerContents>().Items);
        Assert.Empty(bob.Get<Inventory>().Items);
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
        _combatService.AddCombatClaim(orc, alice, currentTick: 1);
        _combatService.AddCombatClaim(orc, bob, currentTick: 5);

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

        _combatService.AddCombatClaim(orc, alice, currentTick: 1);
        // alice died — forfeit her claim
        _combatService.ForfeitClaim(orc, alice);

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
        _combatService.AddCombatClaim(orc, alice, currentTick: 1);

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
                          Threat = new Dictionary<Entity, long>
                          {
                              [alice] = 100,
                              [bob] = 50   // orc also has threat on bob
                          }
                      })
                      .Build();
        orc.Set<ActiveThreatTag>();
        orc.Set(new CombatState { Target = alice });

        _combatService.RemoveFromAllCombat(_f.State, alice);

        // orc's threat on bob must survive — only alice's entry should be removed separately
        Assert.False(orc.Has<CombatState>());
        Assert.True(orc.Get<ThreatTable>().Threat.ContainsKey(bob));  // bob's threat intact
        Assert.Equal(50, orc.Get<ThreatTable>().Threat[bob]);
    }
}

// -------------------------------------------------------------------------
// Test doubles
// -------------------------------------------------------------------------

internal class TestFollowService : IFollowService
{
    public HashSet<Entity> StopFollowingCalled { get; } = [];
    public HashSet<Entity> StopAllFollowersCalled { get; } = [];

    public void Follow(Entity follower, Entity leader) { }

    public void StopFollowing(Entity follower)
        => StopFollowingCalled.Add(follower);

    public void StopAllFollowers(Entity leader)
        => StopAllFollowersCalled.Add(leader);
}
