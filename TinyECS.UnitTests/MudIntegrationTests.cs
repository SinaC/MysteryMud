using TinyECS.Extensions;

namespace TinyECS.UnitTests;

/// <summary>
/// End-to-end tests that exercise World + ComponentStore + Query together
/// using the real MUD component definitions from Components.cs.
/// Each test mirrors a concrete tick-loop scenario from the MUD.
/// </summary>
public class MudIntegrationTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static World NewWorld() => new();

    private static EntityId MakePlayer(World world, int hp = 100, int level = 10)
    {
        var e = world.CreateEntity();
        world.Set<PlayerControlled>(e);
        world.Set(e, new Health { Current = hp, Max = hp });
        world.Set(e, new Mana { Current = 80, Max = 80 });
        world.Set(e, new Level { Value = level });
        world.Set(e, new Position { RoomVnum = 3001 });
        return e;
    }

    private static EntityId MakeMob(World world, int hp = 40)
    {
        var e = world.CreateEntity();
        world.Set<Mobile>(e);
        world.Set(e, new Health { Current = hp, Max = hp });
        world.Set(e, new Position { RoomVnum = 3001 });
        return e;
    }

    private static void StartFight(World world, EntityId aggressor, EntityId defender)
    {
        world.Set(aggressor, new Fighting { Target = defender });
        world.Set<CombatState>(aggressor);
        if (!world.Has<Fighting>(defender))
        {
            world.Set(defender, new Fighting { Target = aggressor });
            world.Set<CombatState>(defender);
        }
    }

    private static List<EntityId> Collect(Query q)
    {
        var list = new List<EntityId>();
        foreach (var e in q) list.Add(e);
        return list;
    }

    // -------------------------------------------------------------------------
    // Combat state tagging
    // -------------------------------------------------------------------------

    [Fact]
    public void StartFight_BothEntities_GetCombatStateAndFightingRelation()
    {
        var world = NewWorld();
        var player = MakePlayer(world);
        var mob = MakeMob(world);

        StartFight(world, player, mob);

        Assert.True(world.Has<CombatState>(player));
        Assert.True(world.Has<CombatState>(mob));
        Assert.True(world.Has<Fighting>(player));
        Assert.True(world.Has<Fighting>(mob));
        Assert.Equal(mob, world.Get<Fighting>(player).Target);
        Assert.Equal(player, world.Get<Fighting>(mob).Target);
    }

    [Fact]
    public void StartFight_AggressorAlreadyFighting_DefenderDoesNotOverwriteRelation()
    {
        var world = NewWorld();
        var player = MakePlayer(world);
        var mob1 = MakeMob(world);
        var mob2 = MakeMob(world);

        StartFight(world, player, mob1);
        // mob1 is already fighting player; calling StartFight again with mob2
        // should not change mob1's Fighting target
        StartFight(world, mob2, mob1);

        Assert.Equal(player, world.Get<Fighting>(mob1).Target); // unchanged
    }

    // -------------------------------------------------------------------------
    // Combat tick — damage and death
    // -------------------------------------------------------------------------

    [Fact]
    public void CombatTick_ReducesTargetHealth()
    {
        var world = NewWorld();
        var player = MakePlayer(world, hp: 100);
        var mob = MakeMob(world, hp: 100);
        StartFight(world, player, mob);

        int hpBefore = world.Get<Health>(mob).Current;

        // Deal fixed 10 damage from player to mob
        world.Get<Health>(mob).Current -= 10;

        Assert.Equal(hpBefore - 10, world.Get<Health>(mob).Current);
    }

    [Fact]
    public void CombatTick_TargetDies_TaggedDead_FighterReleased()
    {
        var world = NewWorld();
        var player = MakePlayer(world, hp: 100);
        var mob = MakeMob(world, hp: 1);
        StartFight(world, player, mob);

        // Simulate killing blow
        world.Get<Health>(mob).Current -= 999;
        if (world.Get<Health>(mob).Current <= 0)
        {
            world.Set<Dead>(mob);
            world.Remove<Fighting>(player);
            world.Remove<CombatState>(player);
        }

        Assert.True(world.Has<Dead>(mob));
        Assert.False(world.Has<Fighting>(player));
        Assert.False(world.Has<CombatState>(player));
    }

    [Fact]
    public void CombatQuery_ExcludesStunnedAndDead()
    {
        var world = NewWorld();
        var active = MakePlayer(world);
        var stunned = MakeMob(world);
        var dead = MakeMob(world);
        var idle = MakeMob(world);

        world.Set<CombatState>(active);
        world.Set<CombatState>(stunned); world.Set<Stunned>(stunned);
        world.Set<CombatState>(dead); world.Set<Dead>(dead);
        // idle: no CombatState

        var results = Collect(
            new Query(world)
                .With<CombatState>()
                .Without<Stunned>()
                .Without<Dead>()
        );

        Assert.Single(results);
        Assert.Contains(active, results);
    }

    // -------------------------------------------------------------------------
    // Regen tick
    // -------------------------------------------------------------------------

    [Fact]
    public void RegenTick_IncreasesHpAndMana_WhenNotDead()
    {
        var world = NewWorld();
        var player = MakePlayer(world, hp: 100);
        world.Get<Health>(player).Current = 50;
        world.Get<Mana>(player).Current = 20;

        // Simulate regen system
        foreach (var e in new Query(world).With<Health>().Without<Dead>())
        {
            ref Health hp = ref world.Get<Health>(e);
            hp.Current = Math.Min(hp.Current + 5, hp.Max);
        }
        foreach (var e in new Query(world).With<Mana>().Without<Dead>())
        {
            ref Mana mp = ref world.Get<Mana>(e);
            mp.Current = Math.Min(mp.Current + 3, mp.Max);
        }

        Assert.Equal(55, world.Get<Health>(player).Current);
        Assert.Equal(23, world.Get<Mana>(player).Current);
    }

    [Fact]
    public void RegenTick_DoesNotExceedMax()
    {
        var world = NewWorld();
        var player = MakePlayer(world, hp: 100);
        // Already at max
        world.Get<Health>(player).Current = 100;

        foreach (var e in new Query(world).With<Health>().Without<Dead>())
        {
            ref Health hp = ref world.Get<Health>(e);
            hp.Current = Math.Min(hp.Current + 5, hp.Max);
        }

        Assert.Equal(100, world.Get<Health>(player).Current);
    }

    [Fact]
    public void RegenTick_SkipsDead()
    {
        var world = NewWorld();
        var dead = MakeMob(world, hp: 1);
        world.Get<Health>(dead).Current = 1;
        world.Set<Dead>(dead);

        foreach (var e in new Query(world).With<Health>().Without<Dead>())
            world.Get<Health>(e).Current += 5;

        Assert.Equal(1, world.Get<Health>(dead).Current); // unchanged
    }

    // -------------------------------------------------------------------------
    // Tick system — timed tags expire
    // -------------------------------------------------------------------------

    [Fact]
    public void StunExpires_AfterTicksRemaining_ReachesZero()
    {
        var world = NewWorld();
        var mob = MakeMob(world);
        world.Set(mob, new Stunned { TicksRemaining = 2 });

        // Tick 1
        foreach (var e in new Query(world).With<Stunned>())
        {
            ref Stunned s = ref world.Get<Stunned>(e);
            if (--s.TicksRemaining <= 0) world.Remove<Stunned>(e);
        }
        Assert.True(world.Has<Stunned>(mob)); // still stunned after tick 1

        // Tick 2
        foreach (var e in new Query(world).With<Stunned>())
        {
            ref Stunned s = ref world.Get<Stunned>(e);
            if (--s.TicksRemaining <= 0) world.Remove<Stunned>(e);
        }
        Assert.False(world.Has<Stunned>(mob)); // expired after tick 2
    }

    [Fact]
    public void PoisonTick_DealsDamage_ThenExpires()
    {
        var world = NewWorld();
        var mob = MakeMob(world, hp: 100);
        world.Set(mob, new Poisoned { DamagePerTick = 5, TicksRemaining = 3 });

        for (int tick = 0; tick < 3; tick++)
        {
            // Collect to avoid modifying-while-iterating
            var poisoned = Collect(new Query(world).With<Poisoned>().Without<Dead>());
            foreach (var e in poisoned)
            {
                ref Poisoned p = ref world.Get<Poisoned>(e);
                ref Health h = ref world.Get<Health>(e);
                h.Current -= p.DamagePerTick;
                if (--p.TicksRemaining <= 0) world.Remove<Poisoned>(e);
                if (h.Current <= 0) world.Set<Dead>(e);
            }
        }

        Assert.Equal(85, world.Get<Health>(mob).Current);  // 100 - 3×5
        Assert.False(world.Has<Poisoned>(mob));             // expired
        Assert.False(world.Has<Dead>(mob));                 // still alive
    }

    [Fact]
    public void CastingTick_CompletesAndTagRemoved()
    {
        var world = NewWorld();
        var player = MakePlayer(world);
        var target = MakeMob(world);
        world.Set(player, new Casting { SpellId = 42, TicksRemaining = 1, Target = target });

        // One tick
        var casting = Collect(new Query(world).With<Casting>());
        foreach (var e in casting)
        {
            ref Casting c = ref world.Get<Casting>(e);
            if (--c.TicksRemaining <= 0) world.Remove<Casting>(e);
        }

        Assert.False(world.Has<Casting>(player));
    }

    // -------------------------------------------------------------------------
    // Extraction system — Dead entities are purged end-of-tick
    // -------------------------------------------------------------------------

    [Fact]
    public void ExtractionSystem_DestroysAllDeadEntities()
    {
        var world = NewWorld();
        var alive = MakePlayer(world);
        var dead1 = MakeMob(world); world.Set<Dead>(dead1);
        var dead2 = MakeMob(world); world.Set<Dead>(dead2);

        // Collect first — never destroy while iterating
        var toExtract = Collect(new Query(world).With<Dead>());
        foreach (var e in toExtract) world.DestroyEntity(e);

        Assert.True(world.IsAlive(alive));
        Assert.False(world.IsAlive(dead1));
        Assert.False(world.IsAlive(dead2));
    }

    [Fact]
    public void ExtractionSystem_RemovedEntities_NotReturnedBySubsequentQueries()
    {
        var world = NewWorld();
        var mob = MakeMob(world);
        world.Set<Dead>(mob);

        var toExtract = Collect(new Query(world).With<Dead>());
        foreach (var e in toExtract) world.DestroyEntity(e);

        var healthQuery = Collect(new Query(world).With<Health>());
        Assert.Empty(healthQuery);
    }

    // -------------------------------------------------------------------------
    // Relations — Fighting, Following, Charmed, Hunting
    // -------------------------------------------------------------------------

    [Fact]
    public void Fighting_Relation_ValidatedCorrectly()
    {
        var world = NewWorld();
        var player = MakePlayer(world);
        var mob = MakeMob(world);
        StartFight(world, player, mob);

        Assert.True(world.IsAlive(world.Get<Fighting>(player).Target));
        Assert.True(world.IsAlive(world.Get<Fighting>(mob).Target));
    }

    [Fact]
    public void Fighting_Target_Destroyed_RelationBecomesStale()
    {
        var world = NewWorld();
        var player = MakePlayer(world);
        var mob = MakeMob(world);
        StartFight(world, player, mob);

        world.DestroyEntity(mob);

        // Relation component is still on player but target is no longer alive
        Assert.True(world.Has<Fighting>(player));
        Assert.False(world.IsAlive(world.Get<Fighting>(player).Target));
    }

    [Fact]
    public void Following_Relation_FollowerTracksLeader()
    {
        var world = NewWorld();
        var leader = MakePlayer(world);
        var follower = MakeMob(world);

        world.Set(follower, new Following { Leader = leader });

        Assert.True(world.Has<Following>(follower));
        Assert.Equal(leader, world.Get<Following>(follower).Leader);
    }

    [Fact]
    public void Charmed_Relation_StoredAndRetrieved()
    {
        var world = NewWorld();
        var caster = MakePlayer(world);
        var mob = MakeMob(world);

        world.Set(mob, new Charmed { Charmer = caster });

        Assert.True(world.Has<Charmed>(mob));
        Assert.Equal(caster, world.Get<Charmed>(mob).Charmer);
    }

    [Fact]
    public void Hunting_Relation_StoredAndRetrieved()
    {
        var world = NewWorld();
        var hunter = MakeMob(world);
        var prey = MakePlayer(world);

        world.Set(hunter, new Hunting { Target = prey });

        Assert.True(world.Has<Hunting>(hunter));
        Assert.Equal(prey, world.Get<Hunting>(hunter).Target);
    }

    // -------------------------------------------------------------------------
    // Full tick simulation
    // -------------------------------------------------------------------------

    [Fact]
    public void FullTick_PlayerKillsMob_ExtractionLeavesWorldClean()
    {
        var world = NewWorld();
        var player = MakePlayer(world, hp: 100, level: 10);
        var mob = MakeMob(world, hp: 1);

        StartFight(world, player, mob);

        // --- COMBAT SYSTEM ---
        var combatants = Collect(
            new Query(world).With<Fighting>().Without<Stunned>().Without<Dead>()
        );
        foreach (var attacker in combatants)
        {
            if (!world.Has<Fighting>(attacker)) continue;
            var target = world.Get<Fighting>(attacker).Target;
            if (!world.IsAlive(target) || world.Has<Dead>(target))
            {
                world.Remove<Fighting>(attacker);
                world.Remove<CombatState>(attacker);
                continue;
            }
            world.Get<Health>(target).Current -= 10;
            if (world.Get<Health>(target).Current <= 0)
            {
                world.Set<Dead>(target);
                world.Remove<Fighting>(attacker);
                world.Remove<CombatState>(attacker);
            }
        }

        // --- EXTRACTION ---
        var dead = Collect(new Query(world).With<Dead>());
        foreach (var e in dead) world.DestroyEntity(e);

        // Assertions
        Assert.True(world.IsAlive(player));
        Assert.False(world.IsAlive(mob));
        Assert.False(world.Has<CombatState>(player));
        Assert.False(world.Has<Fighting>(player));

        // Only player has Health now
        var survivors = Collect(new Query(world).With<Health>());
        Assert.Single(survivors);
        Assert.Contains(player, survivors);
    }

    [Fact]
    public void MultipleMobs_AllDieInOneTick_ExtractionCleansAll()
    {
        var world = NewWorld();
        var player = MakePlayer(world, hp: 500, level: 50);
        const int mobCount = 10;
        var mobs = new EntityId[mobCount];
        for (int i = 0; i < mobCount; i++)
            mobs[i] = MakeMob(world, hp: 1);

        // Put everyone in combat with player (simplified)
        foreach (var m in mobs)
        {
            world.Set(m, new Fighting { Target = player });
            world.Set<CombatState>(m);
            world.Set<Dead>(m);   // instant-kill for test simplicity
        }

        var toExtract = Collect(new Query(world).With<Dead>());
        Assert.Equal(mobCount, toExtract.Count);
        foreach (var e in toExtract) world.DestroyEntity(e);

        Assert.True(world.IsAlive(player));
        foreach (var m in mobs)
            Assert.False(world.IsAlive(m));
    }

    // -------------------------------------------------------------------------
    // Sanctuary (buff) interaction
    // -------------------------------------------------------------------------

    [Fact]
    public void Sanctuary_HalvesDamage_WhenPresent()
    {
        var world = NewWorld();
        var mob = MakeMob(world, hp: 100);
        world.Set<Sanctuary>(mob);

        int rawDamage = 20;
        int actual = world.Has<Sanctuary>(mob) ? rawDamage / 2 : rawDamage;

        world.Get<Health>(mob).Current -= actual;
        Assert.Equal(90, world.Get<Health>(mob).Current); // 100 - 10
    }

    // -------------------------------------------------------------------------
    // Invisible — queries excluding/including
    // -------------------------------------------------------------------------

    [Fact]
    public void InvisibleEntities_ExcludedFromAwareQuery()
    {
        var world = NewWorld();
        var visible = MakeMob(world);
        var invisible = MakeMob(world);
        world.Set<Invisible>(invisible);

        var results = Collect(new Query(world).With<Health>().Without<Invisible>());
        Assert.DoesNotContain(invisible, results);
        Assert.Contains(visible, results);
    }

    // -------------------------------------------------------------------------
    // WithAny in realistic scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void MudTick_AffectedEntitiesQuery_CombatOrPoisoned()
    {
        // Process entities that are either in combat OR poisoned each tick.
        var world = NewWorld();

        var fighter = world.CreateEntity();
        world.Set(fighter, new Health { Current = 80, Max = 100 });
        world.Set<CombatState>(fighter);

        var poisonedIdle = world.CreateEntity();
        world.Set(poisonedIdle, new Health { Current = 60, Max = 100 });
        world.Set<Poisoned>(poisonedIdle);

        var bothFighterAndPoisoned = world.CreateEntity();
        world.Set(bothFighterAndPoisoned, new Health { Current = 50, Max = 100 });
        world.Set<CombatState>(bothFighterAndPoisoned);
        world.Set<Poisoned>(bothFighterAndPoisoned);

        var idle = world.CreateEntity();
        world.Set(idle, new Health { Current = 100, Max = 100 });

        var dead = world.CreateEntity();
        world.Set(dead, new Health { Current = 5, Max = 100 });
        world.Set<CombatState>(dead); world.Set<Dead>(dead);

        var desc = new QueryDescription()
            .WithAll<Health>()
            .WithAny<CombatState, Poisoned>()
            .WithNone<Dead>();

        world.Query<Health>(in desc,
            (EntityId _, ref Health hp) => hp.Current -= 5);

        Assert.Equal(75, world.Get<Health>(fighter).Current);
        Assert.Equal(55, world.Get<Health>(poisonedIdle).Current);
        Assert.Equal(45, world.Get<Health>(bothFighterAndPoisoned).Current);
        Assert.Equal(100, world.Get<Health>(idle).Current);      // untouched
        Assert.Equal(5, world.Get<Health>(dead).Current);      // untouched
    }

    [Fact]
    public void MudTick_DebuffTick_StunnedOrPoisonedEntities_TakeExtraDamage()
    {
        var world = NewWorld();

        var stunned = world.CreateEntity();
        world.Set(stunned, new Health { Current = 100, Max = 100 });
        world.Set<Stunned>(stunned);

        var poisoned = world.CreateEntity();
        world.Set(poisoned, new Health { Current = 100, Max = 100 });
        world.Set<Poisoned>(poisoned);

        var clean = world.CreateEntity();
        world.Set(clean, new Health { Current = 100, Max = 100 });

        var desc = new QueryDescription()
            .WithAll<Health>()
            .WithAny<Stunned, Poisoned>();

        world.Query<Health>(in desc, (EntityId _, ref Health hp) => hp.Current -= 10);

        Assert.Equal(90, world.Get<Health>(stunned).Current);
        Assert.Equal(90, world.Get<Health>(poisoned).Current);
        Assert.Equal(100, world.Get<Health>(clean).Current);     // untouched
    }
}
