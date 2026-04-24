namespace TinyECS.UnitTests;

public class QueryTests
{
    private struct Health { public int Current; }
    private struct Mana { public int Current; }
    private struct Position { public int X, Y; }
    private struct CombatState { }
    private struct Dead { }
    private struct Stunned { }
    private struct Invisible { }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static World MakeWorld() => new();

    private static List<EntityId> Collect(Query q)
    {
        var list = new List<EntityId>();
        foreach (var e in q) list.Add(e);
        return list;
    }

    // -------------------------------------------------------------------------
    // Empty / no-constraint queries
    // -------------------------------------------------------------------------

    [Fact]
    public void Query_NoWith_YieldsNothing()
    {
        var world = MakeWorld();
        world.CreateEntity();
        var results = Collect(new Query(world));
        Assert.Empty(results);
    }

    [Fact]
    public void Query_EmptyWorld_YieldsNothing()
    {
        var world = MakeWorld();
        var results = Collect(new Query(world).With<Health>());
        Assert.Empty(results);
    }

    // -------------------------------------------------------------------------
    // Single With<T>
    // -------------------------------------------------------------------------

    [Fact]
    public void SingleWith_MatchesExactEntities()
    {
        var world = MakeWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);
        var e3 = world.CreateEntity(); // no Health

        var results = Collect(new Query(world).With<Health>());
        Assert.Equal(2, results.Count);
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
        Assert.DoesNotContain(e3, results);
    }

    [Fact]
    public void SingleWith_NoMatch_YieldsEmpty()
    {
        var world = MakeWorld();
        world.CreateEntity(); // alive but no components

        var results = Collect(new Query(world).With<Health>());
        Assert.Empty(results);
    }

    // -------------------------------------------------------------------------
    // Multiple With<T> — intersection
    // -------------------------------------------------------------------------

    [Fact]
    public void TwoWith_ReturnsOnlyIntersection()
    {
        var world = MakeWorld();
        var e1 = world.CreateEntity();
        world.Set<Health>(e1); world.Set<Mana>(e1);   // both

        var e2 = world.CreateEntity();
        world.Set<Health>(e2);                          // Health only

        var e3 = world.CreateEntity();
        world.Set<Mana>(e3);                            // Mana only

        var e4 = world.CreateEntity();                  // neither

        var results = Collect(new Query(world).With<Health>().With<Mana>());
        Assert.Single(results);
        Assert.Contains(e1, results);
    }

    [Fact]
    public void ThreeWith_AllMustMatch()
    {
        var world = MakeWorld();
        var e1 = world.CreateEntity();
        world.Set<Health>(e1); world.Set<Mana>(e1); world.Set<Position>(e1);

        var e2 = world.CreateEntity();
        world.Set<Health>(e2); world.Set<Mana>(e2);   // missing Position

        var results = Collect(new Query(world).With<Health>().With<Mana>().With<Position>());
        Assert.Single(results);
        Assert.Contains(e1, results);
    }

    // -------------------------------------------------------------------------
    // Without<T> — exclusion
    // -------------------------------------------------------------------------

    [Fact]
    public void Without_ExcludesMatchingEntities()
    {
        var world = MakeWorld();
        var alive = world.CreateEntity();
        world.Set<Health>(alive);

        var dead = world.CreateEntity();
        world.Set<Health>(dead);
        world.Set<Dead>(dead);

        var results = Collect(new Query(world).With<Health>().Without<Dead>());
        Assert.Single(results);
        Assert.Contains(alive, results);
        Assert.DoesNotContain(dead, results);
    }

    [Fact]
    public void MultipleWithout_AllExcluded()
    {
        var world = MakeWorld();
        var clean = world.CreateEntity();
        world.Set<Health>(clean);

        var stunned = world.CreateEntity();
        world.Set<Health>(stunned); world.Set<Stunned>(stunned);

        var invisible = world.CreateEntity();
        world.Set<Health>(invisible); world.Set<Invisible>(invisible);

        var both = world.CreateEntity();
        world.Set<Health>(both); world.Set<Stunned>(both); world.Set<Invisible>(both);

        var results = Collect(new Query(world).With<Health>().Without<Stunned>().Without<Invisible>());
        Assert.Single(results);
        Assert.Contains(clean, results);
    }

    [Fact]
    public void Without_WhenNooneHasTag_AllIncluded()
    {
        var world = MakeWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);

        // No EntityId has Dead — everyone should pass the Without<Dead> filter
        var results = Collect(new Query(world).With<Health>().Without<Dead>());
        Assert.Equal(2, results.Count);
    }

    // -------------------------------------------------------------------------
    // Pivot optimisation — smallest store is chosen
    // -------------------------------------------------------------------------

    [Fact]
    public void Pivot_SmallestStore_StillReturnsCorrectResults()
    {
        var world = MakeWorld();

        // Health: 100 entities, Mana: 2 entities — pivot should be Mana
        for (int i = 0; i < 100; i++)
        {
            var e = world.CreateEntity();
            world.Set<Health>(e);
        }

        var e1 = world.CreateEntity(); world.Set<Health>(e1); world.Set<Mana>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2); world.Set<Mana>(e2);

        var results = Collect(new Query(world).With<Health>().With<Mana>());
        Assert.Equal(2, results.Count);
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
    }

    // -------------------------------------------------------------------------
    // Stale EntityId handling
    // -------------------------------------------------------------------------

    [Fact]
    public void DestroyedEntity_NotReturnedByQuery()
    {
        var world = MakeWorld();
        var alive = world.CreateEntity(); world.Set<Health>(alive);
        var dying = world.CreateEntity(); world.Set<Health>(dying);

        world.DestroyEntity(dying);

        var results = Collect(new Query(world).With<Health>());
        Assert.Single(results);
        Assert.Contains(alive, results);
    }

    [Fact]
    public void EntityDestroyedDuringTick_NotVisited_WhenCollectedFirst()
    {
        // Pattern: collect all, then destroy — safe iteration idiom
        var world = MakeWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);
        var e3 = world.CreateEntity(); world.Set<Health>(e3);

        // Collect first
        var toDestroy = Collect(new Query(world).With<Health>());
        Assert.Equal(3, toDestroy.Count);

        // Destroy all
        foreach (var e in toDestroy) world.DestroyEntity(e);

        // Next query should be empty
        var afterResults = Collect(new Query(world).With<Health>());
        Assert.Empty(afterResults);
    }

    // -------------------------------------------------------------------------
    // Tag add/remove between queries (the MUD use-case)
    // -------------------------------------------------------------------------

    [Fact]
    public void TagAddedMidTick_AppearsInNextQuery()
    {
        var world = MakeWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);

        // Before combat tag
        var before = Collect(new Query(world).With<Health>().With<CombatState>());
        Assert.Empty(before);

        world.Set<CombatState>(e);

        var after = Collect(new Query(world).With<Health>().With<CombatState>());
        Assert.Single(after);
        Assert.Contains(e, after);
    }

    [Fact]
    public void TagRemovedMidTick_AbsentFromNextQuery()
    {
        var world = MakeWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Set<CombatState>(e);

        var before = Collect(new Query(world).With<CombatState>());
        Assert.Single(before);

        world.Remove<CombatState>(e);

        var after = Collect(new Query(world).With<CombatState>());
        Assert.Empty(after);
    }

    // -------------------------------------------------------------------------
    // Query reuse across ticks
    // -------------------------------------------------------------------------

    [Fact]
    public void QueryReusedAcrossTicks_ReflectsCurrentState()
    {
        var world = MakeWorld();
        var q = new Query(world).With<Health>().Without<Dead>();

        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);

        // Tick 1: both alive
        var tick1 = Collect(q);
        Assert.Equal(2, tick1.Count);

        // Tick 2: e1 dies
        world.Set<Dead>(e1);
        var tick2 = Collect(q);
        Assert.Single(tick2);
        Assert.Contains(e2, tick2);

        // Tick 3: e2 dies
        world.Set<Dead>(e2);
        var tick3 = Collect(q);
        Assert.Empty(tick3);
    }

    // -------------------------------------------------------------------------
    // Each EntityId visited exactly once
    // -------------------------------------------------------------------------

    [Fact]
    public void Query_NoDuplicates()
    {
        var world = MakeWorld();
        for (int i = 0; i < 20; i++)
        {
            var e = world.CreateEntity();
            world.Set<Health>(e);
            world.Set<Mana>(e);
        }

        var results = Collect(new Query(world).With<Health>().With<Mana>());
        Assert.Equal(results.Count, results.Distinct().Count());
    }

    // -------------------------------------------------------------------------
    // With-only store that is empty
    // -------------------------------------------------------------------------

    [Fact]
    public void With_EmptyStore_YieldsNothing_EvenIfOtherStoreFull()
    {
        var world = MakeWorld();

        // 50 entities with Health but no Mana
        for (int i = 0; i < 50; i++)
        {
            var e = world.CreateEntity();
            world.Set<Health>(e);
        }

        var results = Collect(new Query(world).With<Health>().With<Mana>());
        Assert.Empty(results);
    }

    // -------------------------------------------------------------------------
    // Without on an unregistered store (no store yet for that type)
    // -------------------------------------------------------------------------

    [Fact]
    public void Without_UnregisteredType_ExcludesNothing()
    {
        var world = MakeWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);

        // Invisible has never been Set on any EntityId — its store doesn't exist yet
        var results = Collect(new Query(world).With<Health>().Without<Invisible>());
        Assert.Equal(2, results.Count);
    }

    // -------------------------------------------------------------------------
    // MUD combat scenario integration
    // -------------------------------------------------------------------------

    [Fact]
    public void CombatScenario_CorrectEntitiesProcessed()
    {
        var world = MakeWorld();

        // Player: alive, in combat, not stunned
        var player = world.CreateEntity();
        world.Set<Health>(player);
        world.Set<CombatState>(player);

        // Mob: alive, in combat, stunned
        var stunned = world.CreateEntity();
        world.Set<Health>(stunned);
        world.Set<CombatState>(stunned);
        world.Set<Stunned>(stunned);

        // Idle NPC: not in combat
        var idle = world.CreateEntity();
        world.Set<Health>(idle);

        // Dead mob: in combat store but marked dead
        var dead = world.CreateEntity();
        world.Set<Health>(dead);
        world.Set<CombatState>(dead);
        world.Set<Dead>(dead);

        var combatants = Collect(
            new Query(world)
                .With<Health>()
                .With<CombatState>()
                .Without<Stunned>()
                .Without<Dead>()
        );

        Assert.Single(combatants);
        Assert.Contains(player, combatants);
    }
}