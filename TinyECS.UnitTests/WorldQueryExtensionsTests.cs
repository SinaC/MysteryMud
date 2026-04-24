using TinyECS.Extensions;

namespace TinyECS.UnitTests;

/// <summary>
/// Tests for <see cref="QueryDescription"/> and the
/// <see cref="WorldQueryExtensions"/> delegate-based Query overloads.
///
/// Covers:
///   • QueryDescription builder (WithAll / WithAny / WithNone arities, fluent chaining)
///   • Query&lt;T1&gt;        — single-component
///   • Query&lt;T1,T2&gt;     — two-component, pivot logic
///   • Query&lt;T1,T2,T3&gt;  — three-component, all pivot positions
///   • Query&lt;T1,T2,T3,T4&gt; — four-component, all pivot positions
///   • WithNone exclusion on every arity
///   • WithAny (at-least-one) filtering on every arity
///   • Combined WithAll + WithAny + WithNone on all arities
///   • Ref semantics — mutations inside the callback persist
///   • Stale / destroyed entity skipping
///   • Empty-store / no-match edge cases
///   • Deduplication — each entity visited exactly once
///   • Pivot correctness — smallest store is iterated
///   • Component independence — only matching components are touched
///   • Regression: existing WithNone/WithAll behaviour unchanged after WithAny addition
/// </summary>
public class WorldQueryExtensionsTests
{
    // =========================================================================
    // Component stubs
    // =========================================================================

    private struct Position { public int X, Y; }
    private struct Velocity { public int Dx, Dy; }
    private struct Health { public int Current, Max; }
    private struct Mana { public int Current, Max; }
    private struct Energy { public int Current, Max; }
    private struct Dead { }
    private struct Stunned { }
    private struct Invisible { }
    private struct Marker { }   // generic tag — used where a 4th type is needed
    private struct Poisoned { }   // used in WithAny scenarios
    private struct CombatState { } // used in WithAny scenarios

    // =========================================================================
    // Helpers
    // =========================================================================

    private static World NewWorld() => new();

    /// <summary>Collect every EntityId visited by a Query&lt;T1&gt; call.</summary>
    private static List<EntityId> RunT1<T1>(
        World world, QueryDescription desc)
    {
        var seen = new List<EntityId>();
        world.Query<T1>(in desc, (EntityId e, ref T1 _) => seen.Add(e));
        return seen;
    }

    private static List<EntityId> RunT2<T1, T2>(
        World world, QueryDescription desc)
    {
        var seen = new List<EntityId>();
        world.Query<T1, T2>(in desc, (EntityId e, ref T1 _, ref T2 __) => seen.Add(e));
        return seen;
    }

    private static List<EntityId> RunT3<T1, T2, T3>(
        World world, QueryDescription desc)
    {
        var seen = new List<EntityId>();
        world.Query<T1, T2, T3>(in desc,
            (EntityId e, ref T1 _, ref T2 __, ref T3 ___) => seen.Add(e));
        return seen;
    }

    private static List<EntityId> RunT4<T1, T2, T3, T4>(
        World world, QueryDescription desc)
    {
        var seen = new List<EntityId>();
        world.Query<T1, T2, T3, T4>(in desc,
            (EntityId e, ref T1 _, ref T2 __, ref T3 ___, ref T4 ____) => seen.Add(e));
        return seen;
    }

    //private static List<EntityId> RunT5<T1, T2, T3, T4, T5>(
    //    World world, QueryDescription desc)
    //{
    //    var seen = new List<EntityId>();
    //    world.Query<T1, T2, T3, T4,T5>(in desc,
    //        (EntityId e, ref T1 _, ref T2 __, ref T3 ___, ref T4 ____, ref T5 _____) => seen.Add(e));
    //    return seen;
    //}

    // =========================================================================
    // QueryDescription builder
    // =========================================================================

    [Fact]
    public void QueryDescription_DefaultState_IsEmpty()
    {
        var desc = new QueryDescription();
        Assert.Empty(desc.All);
        Assert.Empty(desc.Any);
        Assert.Empty(desc.None);
    }

    [Fact]
    public void WithAll_SingleType_AddsToAllList()
    {
        var desc = new QueryDescription().WithAll<Position>();
        Assert.Single(desc.All);
        Assert.Contains(typeof(Position), desc.All);
    }

    [Fact]
    public void WithAll_TwoTypes_AddsBoth()
    {
        var desc = new QueryDescription().WithAll<Position, Velocity>();
        Assert.Equal(2, desc.All.Count);
        Assert.Contains(typeof(Position), desc.All);
        Assert.Contains(typeof(Velocity), desc.All);
    }

    [Fact]
    public void WithAll_ThreeTypes_AddsAll()
    {
        var desc = new QueryDescription().WithAll<Position, Velocity, Health>();
        Assert.Equal(3, desc.All.Count);
    }

    [Fact]
    public void WithAll_FourTypes_AddsAll()
    {
        var desc = new QueryDescription().WithAll<Position, Velocity, Health, Mana>();
        Assert.Equal(4, desc.All.Count);
    }

    [Fact]
    public void WithNone_SingleType_AddsToNoneList()
    {
        var desc = new QueryDescription().WithNone<Dead>();
        Assert.Single(desc.None);
        Assert.Contains(typeof(Dead), desc.None);
    }

    [Fact]
    public void WithNone_TwoTypes_AddsBoth()
    {
        var desc = new QueryDescription().WithNone<Dead, Stunned>();
        Assert.Equal(2, desc.None.Count);
    }

    [Fact]
    public void WithNone_ThreeTypes_AddsAll()
    {
        var desc = new QueryDescription().WithNone<Dead, Stunned, Invisible>();
        Assert.Equal(3, desc.None.Count);
    }

    [Fact]
    public void QueryDescription_FluentChain_WithAllAndWithNone()
    {
        var desc = new QueryDescription()
            .WithAll<Position, Velocity>()
            .WithNone<Dead, Stunned>();

        Assert.Equal(2, desc.All.Count);
        Assert.Equal(2, desc.None.Count);
    }

    [Fact]
    public void QueryDescription_FluentChain_AllThreeFilters()
    {
        var desc = new QueryDescription()
            .WithAll<Position, Velocity>()
            .WithAny<CombatState, Poisoned>()
            .WithNone<Dead>();

        Assert.Equal(2, desc.All.Count);
        Assert.Equal(2, desc.Any.Count);
        Assert.Single(desc.None);
    }

    [Fact]
    public void QueryDescription_FluentChain_ReturnsThisInstance()
    {
        var desc = new QueryDescription();
        Assert.Same(desc, desc.WithAll<Position>());
        Assert.Same(desc, desc.WithAny<CombatState>());
        Assert.Same(desc, desc.WithNone<Dead>());
    }

    // =========================================================================
    // Query<T1> — single component
    // =========================================================================

    [Fact]
    public void QueryT1_EmptyWorld_VisitsNothing()
    {
        var world = NewWorld();
        var desc = new QueryDescription().WithAll<Position>();
        var seen = RunT1<Position>(world, desc);
        Assert.Empty(seen);
    }

    [Fact]
    public void QueryT1_NoMatchingEntities_VisitsNothing()
    {
        var world = NewWorld();
        world.CreateEntity(); // alive but no components
        var desc = new QueryDescription().WithAll<Position>();
        Assert.Empty(RunT1<Position>(world, desc));
    }

    [Fact]
    public void QueryT1_VisitsAllMatchingEntities()
    {
        var world = NewWorld();
        var e1 = world.CreateEntity(); world.Set<Position>(e1);
        var e2 = world.CreateEntity(); world.Set<Position>(e2);
        var e3 = world.CreateEntity(); // no Position

        var seen = RunT1<Position>(world, new QueryDescription().WithAll<Position>());
        Assert.Equal(2, seen.Count);
        Assert.Contains(e1, seen);
        Assert.Contains(e2, seen);
        Assert.DoesNotContain(e3, seen);
    }

    [Fact]
    public void QueryT1_RefMutation_Persists()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Position { X = 1, Y = 2 });

        var desc = new QueryDescription().WithAll<Position>();
        world.Query<Position>(in desc, (EntityId _, ref Position pos) =>
        {
            pos.X += 10;
            pos.Y += 10;
        });

        Assert.Equal(11, world.Get<Position>(e).X);
        Assert.Equal(12, world.Get<Position>(e).Y);
    }

    [Fact]
    public void QueryT1_WithNone_ExcludesTagged()
    {
        var world = NewWorld();
        var alive = world.CreateEntity();
        world.Set<Position>(alive);

        var dead = world.CreateEntity();
        world.Set<Position>(dead);
        world.Set<Dead>(dead);

        var desc = new QueryDescription().WithAll<Position>().WithNone<Dead>();
        var seen = RunT1<Position>(world, desc);

        Assert.Single(seen);
        Assert.Contains(alive, seen);
    }

    [Fact]
    public void QueryT1_WithNone_MultipleExclusions()
    {
        var world = NewWorld();
        var clean = world.CreateEntity(); world.Set<Position>(clean);
        var stunned = world.CreateEntity(); world.Set<Position>(stunned); world.Set<Stunned>(stunned);
        var dead = world.CreateEntity(); world.Set<Position>(dead); world.Set<Dead>(dead);

        var desc = new QueryDescription().WithAll<Position>().WithNone<Dead, Stunned>();
        var seen = RunT1<Position>(world, desc);

        Assert.Single(seen);
        Assert.Contains(clean, seen);
    }

    [Fact]
    public void QueryT1_SkipsDestroyedEntities()
    {
        var world = NewWorld();
        var alive = world.CreateEntity(); world.Set<Position>(alive);
        var dead = world.CreateEntity(); world.Set<Position>(dead);
        world.DestroyEntity(dead);

        var seen = RunT1<Position>(world, new QueryDescription().WithAll<Position>());
        Assert.Single(seen);
        Assert.Contains(alive, seen);
    }

    [Fact]
    public void QueryT1_NoDuplicates()
    {
        var world = NewWorld();
        for (int i = 0; i < 50; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e);
        }
        var seen = RunT1<Position>(world, new QueryDescription().WithAll<Position>());
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    // =========================================================================
    // Query<T1, T2> — two components
    // =========================================================================

    [Fact]
    public void QueryT2_EmptyWorld_VisitsNothing()
    {
        var world = NewWorld();
        var desc = new QueryDescription().WithAll<Position, Velocity>();
        Assert.Empty(RunT2<Position, Velocity>(world, desc));
    }

    [Fact]
    public void QueryT2_RequiresBothComponents()
    {
        var world = NewWorld();
        var both = world.CreateEntity();
        world.Set<Position>(both); world.Set<Velocity>(both);

        var posOnly = world.CreateEntity(); world.Set<Position>(posOnly);
        var velOnly = world.CreateEntity(); world.Set<Velocity>(velOnly);
        var neither = world.CreateEntity();

        var desc = new QueryDescription().WithAll<Position, Velocity>();
        var seen = RunT2<Position, Velocity>(world, desc);

        Assert.Single(seen);
        Assert.Contains(both, seen);
    }

    [Fact]
    public void QueryT2_RefMutation_BothComponentsPersist()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Position { X = 0, Y = 0 });
        world.Set(e, new Velocity { Dx = 3, Dy = 4 });

        var desc = new QueryDescription().WithAll<Position, Velocity>();
        world.Query<Position, Velocity>(in desc,
            (EntityId _, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.Dx;
                pos.Y += vel.Dy;
            });

        Assert.Equal(3, world.Get<Position>(e).X);
        Assert.Equal(4, world.Get<Position>(e).Y);
    }

    [Fact]
    public void QueryT2_WithNone_ExcludesTagged()
    {
        var world = NewWorld();
        var active = world.CreateEntity();
        world.Set<Position>(active); world.Set<Velocity>(active);

        var stunned = world.CreateEntity();
        world.Set<Position>(stunned); world.Set<Velocity>(stunned); world.Set<Stunned>(stunned);

        var desc = new QueryDescription().WithAll<Position, Velocity>().WithNone<Stunned>();
        var seen = RunT2<Position, Velocity>(world, desc);

        Assert.Single(seen);
        Assert.Contains(active, seen);
    }

    [Fact]
    public void QueryT2_SkipsDestroyedEntities()
    {
        var world = NewWorld();
        var alive = world.CreateEntity();
        world.Set<Position>(alive); world.Set<Velocity>(alive);

        var dying = world.CreateEntity();
        world.Set<Position>(dying); world.Set<Velocity>(dying);
        world.DestroyEntity(dying);

        var seen = RunT2<Position, Velocity>(world, new QueryDescription().WithAll<Position, Velocity>());
        Assert.Single(seen);
        Assert.Contains(alive, seen);
    }

    [Fact]
    public void QueryT2_NoDuplicates()
    {
        var world = NewWorld();
        for (int i = 0; i < 30; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e);
            world.Set<Velocity>(e);
        }
        var seen = RunT2<Position, Velocity>(world, new QueryDescription().WithAll<Position, Velocity>());
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    // -------------------------------------------------------------------------
    // Pivot correctness for T2
    // -------------------------------------------------------------------------

    [Fact]
    public void QueryT2_PivotIsSmallestStore_S1Smaller()
    {
        // s1 (Position) = 1 entity, s2 (Velocity) = 100 entities
        // Pivot should be s1 — only entity with both should be visited.
        var world = NewWorld();

        var both = world.CreateEntity();
        world.Set<Position>(both);
        world.Set<Velocity>(both);

        for (int i = 0; i < 99; i++)
        {
            var e = world.CreateEntity();
            world.Set<Velocity>(e); // Velocity only — no Position
        }

        var seen = RunT2<Position, Velocity>(world, new QueryDescription().WithAll<Position, Velocity>());
        Assert.Single(seen);
        Assert.Contains(both, seen);
    }

    [Fact]
    public void QueryT2_PivotIsSmallestStore_S2Smaller()
    {
        // s1 (Position) = 100 entities, s2 (Velocity) = 1 entity
        // Pivot should be s2.
        var world = NewWorld();

        var both = world.CreateEntity();
        world.Set<Position>(both);
        world.Set<Velocity>(both);

        for (int i = 0; i < 99; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e); // Position only — no Velocity
        }

        var seen = RunT2<Position, Velocity>(world, new QueryDescription().WithAll<Position, Velocity>());
        Assert.Single(seen);
        Assert.Contains(both, seen);
    }

    [Fact]
    public void QueryT2_EqualSizeStores_PivotIsS1()
    {
        // When counts are equal, s1 wins (pivotIs1 = s1.Count <= s2.Count).
        // Result must still be correct regardless of which wins.
        var world = NewWorld();
        var e1 = world.CreateEntity(); world.Set<Position>(e1); world.Set<Velocity>(e1);
        var e2 = world.CreateEntity(); world.Set<Position>(e2); world.Set<Velocity>(e2);

        var seen = RunT2<Position, Velocity>(world, new QueryDescription().WithAll<Position, Velocity>());
        Assert.Equal(2, seen.Count);
        Assert.Contains(e1, seen);
        Assert.Contains(e2, seen);
    }

    // =========================================================================
    // Query<T1, T2, T3> — three components
    // =========================================================================

    [Fact]
    public void QueryT3_RequiresAllThree()
    {
        var world = NewWorld();
        var all3 = world.CreateEntity();
        world.Set<Position>(all3); world.Set<Velocity>(all3); world.Set<Health>(all3);

        var two = world.CreateEntity();
        world.Set<Position>(two); world.Set<Velocity>(two);   // missing Health

        var desc = new QueryDescription().WithAll<Position, Velocity, Health>();
        var seen = RunT3<Position, Velocity, Health>(world, desc);

        Assert.Single(seen);
        Assert.Contains(all3, seen);
    }

    [Fact]
    public void QueryT3_RefMutation_AllThreeComponentsPersist()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { Dx = 2 });
        world.Set(e, new Health { Current = 10, Max = 100 });

        var desc = new QueryDescription().WithAll<Position, Velocity, Health>();
        world.Query<Position, Velocity, Health>(in desc,
            (EntityId _, ref Position pos, ref Velocity vel, ref Health hp) =>
            {
                pos.X += 10;
                vel.Dx += 10;
                hp.Current -= 3;
            });

        Assert.Equal(11, world.Get<Position>(e).X);
        Assert.Equal(12, world.Get<Velocity>(e).Dx);
        Assert.Equal(7, world.Get<Health>(e).Current);
    }

    [Fact]
    public void QueryT3_WithNone_ExcludesCorrectly()
    {
        var world = NewWorld();
        var ok = world.CreateEntity();
        world.Set<Position>(ok); world.Set<Velocity>(ok); world.Set<Health>(ok);

        var bad = world.CreateEntity();
        world.Set<Position>(bad); world.Set<Velocity>(bad); world.Set<Health>(bad);
        world.Set<Dead>(bad);

        var desc = new QueryDescription().WithAll<Position, Velocity, Health>().WithNone<Dead>();
        var seen = RunT3<Position, Velocity, Health>(world, desc);

        Assert.Single(seen);
        Assert.Contains(ok, seen);
    }

    // -------------------------------------------------------------------------
    // Pivot for T3 — all three positions tested
    // -------------------------------------------------------------------------

    [Fact]
    public void QueryT3_PivotIndex0_S1Smallest()
    {
        var world = NewWorld();

        // 1 entity has all 3. s1 (Position) = 1, s2 (Velocity) = 5, s3 (Health) = 5
        var both = world.CreateEntity();
        world.Set<Position>(both); world.Set<Velocity>(both); world.Set<Health>(both);

        for (int i = 0; i < 4; i++)
        {
            var e = world.CreateEntity();
            world.Set<Velocity>(e); world.Set<Health>(e);
        }

        var seen = RunT3<Position, Velocity, Health>(world, new QueryDescription().WithAll<Position, Velocity, Health>());
        Assert.Single(seen);
        Assert.Contains(both, seen);
    }

    [Fact]
    public void QueryT3_PivotIndex1_S2Smallest()
    {
        var world = NewWorld();

        var both = world.CreateEntity();
        world.Set<Position>(both); world.Set<Velocity>(both); world.Set<Health>(both);

        for (int i = 0; i < 4; i++) { var e = world.CreateEntity(); world.Set<Position>(e); world.Set<Health>(e); }
        // s1=5, s2=1, s3=5 — pivot should be s2

        var seen = RunT3<Position, Velocity, Health>(world, new QueryDescription().WithAll<Position, Velocity, Health>());
        Assert.Single(seen);
        Assert.Contains(both, seen);
    }

    [Fact]
    public void QueryT3_PivotIndex2_S3Smallest()
    {
        var world = NewWorld();

        var both = world.CreateEntity();
        world.Set<Position>(both); world.Set<Velocity>(both); world.Set<Health>(both);

        for (int i = 0; i < 4; i++) { var e = world.CreateEntity(); world.Set<Position>(e); world.Set<Velocity>(e); }
        // s1=5, s2=5, s3=1 — pivot should be s3

        var seen = RunT3<Position, Velocity, Health>(world, new QueryDescription().WithAll<Position, Velocity, Health>());
        Assert.Single(seen);
        Assert.Contains(both, seen);
    }

    [Fact]
    public void QueryT3_NoDuplicates()
    {
        var world = NewWorld();
        for (int i = 0; i < 20; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e); world.Set<Velocity>(e); world.Set<Health>(e);
        }
        var seen = RunT3<Position, Velocity, Health>(world, new QueryDescription().WithAll<Position, Velocity, Health>());
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    // =========================================================================
    // Query<T1, T2, T3, T4> — four components
    // =========================================================================

    [Fact]
    public void QueryT4_RequiresAllFour()
    {
        var world = NewWorld();
        var all4 = world.CreateEntity();
        world.Set<Position>(all4); world.Set<Velocity>(all4);
        world.Set<Health>(all4); world.Set<Mana>(all4);

        var three = world.CreateEntity();
        world.Set<Position>(three); world.Set<Velocity>(three); world.Set<Health>(three);

        var desc = new QueryDescription().WithAll<Position, Velocity, Health, Mana>();
        var seen = RunT4<Position, Velocity, Health, Mana>(world, desc);

        Assert.Single(seen);
        Assert.Contains(all4, seen);
    }

    [Fact]
    public void QueryT4_RefMutation_AllFourComponentsPersist()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { Dx = 2 });
        world.Set(e, new Health { Current = 10 });
        world.Set(e, new Mana { Current = 20 });

        var desc = new QueryDescription().WithAll<Position, Velocity, Health, Mana>();
        world.Query<Position, Velocity, Health, Mana>(in desc,
            (EntityId _, ref Position pos, ref Velocity vel, ref Health hp, ref Mana mp) =>
            {
                pos.X += 1; vel.Dx += 1; hp.Current += 1; mp.Current += 1;
            });

        Assert.Equal(2, world.Get<Position>(e).X);
        Assert.Equal(3, world.Get<Velocity>(e).Dx);
        Assert.Equal(11, world.Get<Health>(e).Current);
        Assert.Equal(21, world.Get<Mana>(e).Current);
    }

    [Fact]
    public void QueryT4_WithNone_ExcludesCorrectly()
    {
        var world = NewWorld();
        var ok = world.CreateEntity();
        world.Set<Position>(ok); world.Set<Velocity>(ok); world.Set<Health>(ok); world.Set<Mana>(ok);

        var bad = world.CreateEntity();
        world.Set<Position>(bad); world.Set<Velocity>(bad); world.Set<Health>(bad); world.Set<Mana>(bad);
        world.Set<Stunned>(bad);

        var desc = new QueryDescription()
            .WithAll<Position, Velocity, Health, Mana>()
            .WithNone<Stunned>();
        var seen = RunT4<Position, Velocity, Health, Mana>(world, desc);

        Assert.Single(seen);
        Assert.Contains(ok, seen);
    }

    // -------------------------------------------------------------------------
    // Pivot for T4 — all four positions tested
    // -------------------------------------------------------------------------

    [Fact]
    public void QueryT4_PivotIndex0_S1Smallest()
    {
        var world = NewWorld();
        var all4 = world.CreateEntity();
        world.Set<Position>(all4); world.Set<Velocity>(all4); world.Set<Health>(all4); world.Set<Mana>(all4);

        // Add extras to s2, s3, s4 — s1 stays at count=1
        for (int i = 0; i < 4; i++)
        {
            var e = world.CreateEntity();
            world.Set<Velocity>(e); world.Set<Health>(e); world.Set<Mana>(e);
        }

        var seen = RunT4<Position, Velocity, Health, Mana>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana>());
        Assert.Single(seen);
        Assert.Contains(all4, seen);
    }

    [Fact]
    public void QueryT4_PivotIndex1_S2Smallest()
    {
        var world = NewWorld();
        var all4 = world.CreateEntity();
        world.Set<Position>(all4); world.Set<Velocity>(all4); world.Set<Health>(all4); world.Set<Mana>(all4);

        for (int i = 0; i < 4; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e); world.Set<Health>(e); world.Set<Mana>(e); // s2 (Velocity) stays 1
        }

        var seen = RunT4<Position, Velocity, Health, Mana>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana>());
        Assert.Single(seen);
        Assert.Contains(all4, seen);
    }

    [Fact]
    public void QueryT4_PivotIndex2_S3Smallest()
    {
        var world = NewWorld();
        var all4 = world.CreateEntity();
        world.Set<Position>(all4); world.Set<Velocity>(all4); world.Set<Health>(all4); world.Set<Mana>(all4);

        for (int i = 0; i < 4; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e); world.Set<Velocity>(e); world.Set<Mana>(e); // s3 (Health) stays 1
        }

        var seen = RunT4<Position, Velocity, Health, Mana>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana>());
        Assert.Single(seen);
        Assert.Contains(all4, seen);
    }

    [Fact]
    public void QueryT4_PivotIndex3_S4Smallest()
    {
        var world = NewWorld();
        var all4 = world.CreateEntity();
        world.Set<Position>(all4); world.Set<Velocity>(all4); world.Set<Health>(all4); world.Set<Mana>(all4);

        for (int i = 0; i < 4; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e); world.Set<Velocity>(e); world.Set<Health>(e); // s4 (Mana) stays 1
        }

        var seen = RunT4<Position, Velocity, Health, Mana>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana>());
        Assert.Single(seen);
        Assert.Contains(all4, seen);
    }

    [Fact]
    public void QueryT4_NoDuplicates()
    {
        var world = NewWorld();
        for (int i = 0; i < 20; i++)
        {
            var e = world.CreateEntity();
            world.Set<Position>(e); world.Set<Velocity>(e);
            world.Set<Health>(e); world.Set<Mana>(e);
        }
        var seen = RunT4<Position, Velocity, Health, Mana>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana>());
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    // =========================================================================
    // Query<T1, T2, T3, T4, T5> — give components
    // =========================================================================

    //[Fact]
    //public void QueryT5_RequiresAllFive()
    //{
    //    var world = NewWorld();
    //    var all5 = world.CreateEntity();
    //    world.Set<Position>(all5); world.Set<Velocity>(all5);
    //    world.Set<Health>(all5); world.Set<Mana>(all5);
    //    world.Set<Energy>(all5);

    //    var four = world.CreateEntity();
    //    world.Set<Position>(four); world.Set<Velocity>(four); world.Set<Health>(four); world.Set<Mana>(four);

    //    var desc = new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>();
    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, desc);

    //    Assert.Single(seen);
    //    Assert.Contains(all5, seen);
    //}

    //[Fact]
    //public void QueryT5_RefMutation_AllFiveComponentsPersist()
    //{
    //    var world = NewWorld();
    //    var e = world.CreateEntity();
    //    world.Set(e, new Position { X = 1 });
    //    world.Set(e, new Velocity { Dx = 2 });
    //    world.Set(e, new Health { Current = 10 });
    //    world.Set(e, new Mana { Current = 20 });
    //    world.Set(e, new Energy { Current = 30 });

    //    var desc = new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>();
    //    world.Query<Position, Velocity, Health, Mana, Energy>(in desc,
    //        (EntityId _, ref Position pos, ref Velocity vel, ref Health hp, ref Mana mp, ref Energy ep) =>
    //        {
    //            pos.X += 1; vel.Dx += 1; hp.Current += 1; mp.Current += 1; ep.Current += 1;
    //        });

    //    Assert.Equal(2, world.Get<Position>(e).X);
    //    Assert.Equal(3, world.Get<Velocity>(e).Dx);
    //    Assert.Equal(11, world.Get<Health>(e).Current);
    //    Assert.Equal(21, world.Get<Mana>(e).Current);
    //    Assert.Equal(31, world.Get<Energy>(e).Current);
    //}

    //[Fact]
    //public void QueryT5_WithNone_ExcludesCorrectly()
    //{
    //    var world = NewWorld();
    //    var ok = world.CreateEntity();
    //    world.Set<Position>(ok); world.Set<Velocity>(ok); world.Set<Health>(ok); world.Set<Mana>(ok); world.Set<Energy>(ok);

    //    var bad = world.CreateEntity();
    //    world.Set<Position>(bad); world.Set<Velocity>(bad); world.Set<Health>(bad); world.Set<Mana>(bad); world.Set<Energy>(bad);
    //    world.Set<Stunned>(bad);

    //    var desc = new QueryDescription()
    //        .WithAll<Position, Velocity, Health, Mana, Energy>()
    //        .WithNone<Stunned>();
    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, desc);

    //    Assert.Single(seen);
    //    Assert.Contains(ok, seen);
    //}

    // -------------------------------------------------------------------------
    // Pivot for T5 — all five positions tested
    // -------------------------------------------------------------------------

    //[Fact]
    //public void QueryT5_PivotIndex0_S1Smallest()
    //{
    //    var world = NewWorld();
    //    var all5 = world.CreateEntity();
    //    world.Set<Position>(all5); world.Set<Velocity>(all5); world.Set<Health>(all5); world.Set<Mana>(all5); world.Set<Energy>(all5);

    //    // Add extras to s2, s3, s4, s5 — s1 stays at count=1
    //    for (int i = 0; i < 5; i++)
    //    {
    //        var e = world.CreateEntity();
    //        world.Set<Velocity>(e); world.Set<Health>(e); world.Set<Mana>(e); world.Set<Energy>(e);
    //    }

    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>());
    //    Assert.Single(seen);
    //    Assert.Contains(all5, seen);
    //}

    //[Fact]
    //public void QueryT5_PivotIndex1_S2Smallest()
    //{
    //    var world = NewWorld();
    //    var all5 = world.CreateEntity();
    //    world.Set<Position>(all5); world.Set<Velocity>(all5); world.Set<Health>(all5); world.Set<Mana>(all5); world.Set<Energy>(all5);

    //    for (int i = 0; i < 5; i++)
    //    {
    //        var e = world.CreateEntity();
    //        world.Set<Position>(e); world.Set<Health>(e); world.Set<Mana>(e); world.Set<Energy>(e);// s2 (Velocity) stays 1
    //    }

    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>());
    //    Assert.Single(seen);
    //    Assert.Contains(all5, seen);
    //}

    //[Fact]
    //public void QueryT5_PivotIndex2_S3Smallest()
    //{
    //    var world = NewWorld();
    //    var all5 = world.CreateEntity();
    //    world.Set<Position>(all5); world.Set<Velocity>(all5); world.Set<Health>(all5); world.Set<Mana>(all5); world.Set<Energy>(all5);

    //    for (int i = 0; i < 5; i++)
    //    {
    //        var e = world.CreateEntity();
    //        world.Set<Position>(e); world.Set<Velocity>(e); world.Set<Mana>(e); world.Set<Energy>(e);// s3 (Health) stays 1
    //    }

    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>());
    //    Assert.Single(seen);
    //    Assert.Contains(all5, seen);
    //}

    //[Fact]
    //public void QueryT5_PivotIndex3_S4Smallest()
    //{
    //    var world = NewWorld();
    //    var all5 = world.CreateEntity();
    //    world.Set<Position>(all5); world.Set<Velocity>(all5); world.Set<Health>(all5); world.Set<Mana>(all5); world.Set<Energy>(all5);

    //    for (int i = 0; i < 5; i++)
    //    {
    //        var e = world.CreateEntity();
    //        world.Set<Position>(e); world.Set<Velocity>(e); world.Set<Health>(e); world.Set<Energy>(all5); // s4 (Mana) stays 1
    //    }

    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>());
    //    Assert.Single(seen);
    //    Assert.Contains(all5, seen);
    //}

    //[Fact]
    //public void QueryT5_PivotIndex4_S5Smallest()
    //{
    //    var world = NewWorld();
    //    var all5 = world.CreateEntity();
    //    world.Set<Position>(all5); world.Set<Velocity>(all5); world.Set<Health>(all5); world.Set<Mana>(all5); world.Set<Energy>(all5);

    //    for (int i = 0; i < 5; i++)
    //    {
    //        var e = world.CreateEntity();
    //        world.Set<Position>(e); world.Set<Velocity>(e); world.Set<Health>(e); world.Set<Mana>(e); // s5 (Energy) stays 1
    //    }

    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>());
    //    Assert.Single(seen);
    //    Assert.Contains(all5, seen);
    //}

    //[Fact]
    //public void QueryT5_NoDuplicates()
    //{
    //    var world = NewWorld();
    //    for (int i = 0; i < 20; i++)
    //    {
    //        var e = world.CreateEntity();
    //        world.Set<Position>(e); world.Set<Velocity>(e);
    //        world.Set<Health>(e); world.Set<Mana>(e);
    //        world.Set<Energy>(e);
    //    }
    //    var seen = RunT5<Position, Velocity, Health, Mana, Energy>(world, new QueryDescription().WithAll<Position, Velocity, Health, Mana, Energy>());
    //    Assert.Equal(seen.Count, seen.Distinct().Count());
    //}

    // =========================================================================
    // WithNone — cross-arity edge cases
    // =========================================================================

    [Fact]
    public void WithNone_UnregisteredType_ExcludesNothing()
    {
        // If the excluded type was never set on any entity its store doesn't
        // exist yet — StoreByType should create it lazily and return empty.
        var world = NewWorld();
        var e = world.CreateEntity(); world.Set<Position>(e);

        var desc = new QueryDescription().WithAll<Position>().WithNone<Invisible>();
        var seen = RunT1<Position>(world, desc);

        Assert.Single(seen);
        Assert.Contains(e, seen);
    }

    [Fact]
    public void WithNone_TagAddedThenRemoved_EntityReappearsInQuery()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Position>(e);
        world.Set<Velocity>(e);
        world.Set<Stunned>(e);

        var desc = new QueryDescription().WithAll<Position, Velocity>().WithNone<Stunned>();

        // While stunned — excluded
        var before = RunT2<Position, Velocity>(world, desc);
        Assert.Empty(before);

        // Stun removed
        world.Remove<Stunned>(e);
        var after = RunT2<Position, Velocity>(world, desc);
        Assert.Single(after);
        Assert.Contains(e, after);
    }

    [Fact]
    public void WithNone_MultipleExclusions_AllMustBeAbsent()
    {
        var world = NewWorld();
        var clean = world.CreateEntity(); world.Set<Position>(clean);
        var hasDead = world.CreateEntity(); world.Set<Position>(hasDead); world.Set<Dead>(hasDead);
        var hasStun = world.CreateEntity(); world.Set<Position>(hasStun); world.Set<Stunned>(hasStun);
        var hasBoth = world.CreateEntity(); world.Set<Position>(hasBoth); world.Set<Dead>(hasBoth); world.Set<Stunned>(hasBoth);

        var desc = new QueryDescription().WithAll<Position>().WithNone<Dead, Stunned>();
        var seen = RunT1<Position>(world, desc);

        Assert.Single(seen);
        Assert.Contains(clean, seen);
    }

    // =========================================================================
    // Ref semantics — mutations must write back to the store
    // =========================================================================

    [Fact]
    public void RefSemantics_T1_MutationVisibleAfterQuery()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 50, Max = 100 });

        var desc = new QueryDescription().WithAll<Health>();
        world.Query<Health>(in desc, (EntityId _, ref Health hp) => hp.Current = 99);

        Assert.Equal(99, world.Get<Health>(e).Current);
    }

    [Fact]
    public void RefSemantics_T2_BothRefsAliasSameStore()
    {
        // Mutate both refs in the callback and verify both values changed.
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Position { X = 0 });
        world.Set(e, new Velocity { Dx = 5 });

        var desc = new QueryDescription().WithAll<Position, Velocity>();
        world.Query<Position, Velocity>(in desc,
            (EntityId _, ref Position pos, ref Velocity vel) =>
            {
                pos.X = vel.Dx * 2;   // 10
                vel.Dx = 99;
            });

        Assert.Equal(10, world.Get<Position>(e).X);
        Assert.Equal(99, world.Get<Velocity>(e).Dx);
    }

    [Fact]
    public void RefSemantics_T3_AllThreeRefsPersist()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { Dx = 2 });
        world.Set(e, new Health { Current = 3 });

        var desc = new QueryDescription().WithAll<Position, Velocity, Health>();
        world.Query<Position, Velocity, Health>(in desc,
            (EntityId _, ref Position p, ref Velocity v, ref Health h) =>
            { p.X = 10; v.Dx = 20; h.Current = 30; });

        Assert.Equal(10, world.Get<Position>(e).X);
        Assert.Equal(20, world.Get<Velocity>(e).Dx);
        Assert.Equal(30, world.Get<Health>(e).Current);
    }

    [Fact]
    public void RefSemantics_MutationAcrossMultipleEntities()
    {
        var world = NewWorld();
        var ids = new EntityId[5];
        for (int i = 0; i < 5; i++)
        {
            ids[i] = world.CreateEntity();
            world.Set(ids[i], new Health { Current = i * 10, Max = 100 });
        }

        var desc = new QueryDescription().WithAll<Health>();
        world.Query<Health>(in desc, (EntityId _, ref Health hp) => hp.Current += 1);

        for (int i = 0; i < 5; i++)
            Assert.Equal(i * 10 + 1, world.Get<Health>(ids[i]).Current);
    }

    // =========================================================================
    // Query reuse across "ticks"
    // =========================================================================

    [Fact]
    public void QueryDescription_ReusedAcrossTicks_ReflectsCurrentWorld()
    {
        var world = NewWorld();
        var desc = new QueryDescription().WithAll<Position, Velocity>().WithNone<Dead>();

        var e1 = world.CreateEntity(); world.Set<Position>(e1); world.Set<Velocity>(e1);
        var e2 = world.CreateEntity(); world.Set<Position>(e2); world.Set<Velocity>(e2);

        // Tick 1 — both alive
        var tick1 = RunT2<Position, Velocity>(world, desc);
        Assert.Equal(2, tick1.Count);

        // Tick 2 — e1 dies
        world.Set<Dead>(e1);
        var tick2 = RunT2<Position, Velocity>(world, desc);
        Assert.Single(tick2);
        Assert.Contains(e2, tick2);

        // Tick 3 — e1 tag removed, back in play
        world.Remove<Dead>(e1);
        var tick3 = RunT2<Position, Velocity>(world, desc);
        Assert.Equal(2, tick3.Count);
    }

    // =========================================================================
    // Correctness: only matching entities are touched, others unaffected
    // =========================================================================

    [Fact]
    public void Query_OnlyCallbackMatchingEntities_OthersUnchanged()
    {
        var world = NewWorld();

        // These should be touched
        var matched = world.CreateEntity();
        world.Set(matched, new Health { Current = 10 });
        world.Set<Velocity>(matched);

        // This has Health but no Velocity — should NOT be touched
        var noVel = world.CreateEntity();
        world.Set(noVel, new Health { Current = 50 });

        var desc = new QueryDescription().WithAll<Health, Velocity>();
        world.Query<Health, Velocity>(in desc,
            (EntityId _, ref Health hp, ref Velocity __) => hp.Current = 0);

        Assert.Equal(0, world.Get<Health>(matched).Current); // mutated
        Assert.Equal(50, world.Get<Health>(noVel).Current);   // untouched
    }

    // =========================================================================
    // Integration: MUD movement tick
    // =========================================================================

    [Fact]
    public void MovementTick_UpdatesPositionByVelocity_ForAllMovingEntities()
    {
        var world = NewWorld();

        var moving = world.CreateEntity();
        world.Set(moving, new Position { X = 0, Y = 0 });
        world.Set(moving, new Velocity { Dx = 3, Dy = -1 });

        var stunned = world.CreateEntity();
        world.Set(stunned, new Position { X = 5, Y = 5 });
        world.Set(stunned, new Velocity { Dx = 1, Dy = 1 });
        world.Set<Stunned>(stunned);

        var standing = world.CreateEntity();
        world.Set(standing, new Position { X = 10, Y = 10 });
        // No Velocity — not a mover

        var desc = new QueryDescription()
            .WithAll<Position, Velocity>()
            .WithNone<Stunned>();

        world.Query<Position, Velocity>(in desc,
            (EntityId _, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.Dx;
                pos.Y += vel.Dy;
            });

        // Only `moving` should have changed
        Assert.Equal(3, world.Get<Position>(moving).X);
        Assert.Equal(-1, world.Get<Position>(moving).Y);

        Assert.Equal(5, world.Get<Position>(stunned).X);   // unchanged
        Assert.Equal(5, world.Get<Position>(stunned).Y);

        Assert.Equal(10, world.Get<Position>(standing).X);  // unchanged
        Assert.Equal(10, world.Get<Position>(standing).Y);
    }

    [Fact]
    public void CombatRegenTick_ModifiesHealthForLivingCombatants()
    {
        var world = NewWorld();

        var fighter = world.CreateEntity();
        world.Set(fighter, new Health { Current = 80, Max = 100 });
        world.Set(fighter, new Mana { Current = 40, Max = 80 });

        var dead = world.CreateEntity();
        world.Set(dead, new Health { Current = 0, Max = 100 });
        world.Set(dead, new Mana { Current = 0, Max = 80 });
        world.Set<Dead>(dead);

        var desc = new QueryDescription()
            .WithAll<Health, Mana>()
            .WithNone<Dead>();

        world.Query<Health, Mana>(in desc,
            (EntityId _, ref Health hp, ref Mana mp) =>
            {
                hp.Current = Math.Min(hp.Current + 5, hp.Max);
                mp.Current = Math.Min(mp.Current + 3, mp.Max);
            });

        Assert.Equal(85, world.Get<Health>(fighter).Current);
        Assert.Equal(43, world.Get<Mana>(fighter).Current);

        Assert.Equal(0, world.Get<Health>(dead).Current);  // unchanged
        Assert.Equal(0, world.Get<Mana>(dead).Current);
    }

    // =========================================================================
    // QueryDescription — WithAny builder
    // =========================================================================

    [Fact]
    public void WithAny_SingleType_AddsToAnyList()
    {
        var desc = new QueryDescription().WithAny<CombatState>();
        Assert.Single(desc.Any);
        Assert.Contains(typeof(CombatState), desc.Any);
    }

    [Fact]
    public void WithAny_TwoTypes_AddsBoth()
    {
        var desc = new QueryDescription().WithAny<CombatState, Poisoned>();
        Assert.Equal(2, desc.Any.Count);
        Assert.Contains(typeof(CombatState), desc.Any);
        Assert.Contains(typeof(Poisoned), desc.Any);
    }

    [Fact]
    public void WithAny_ThreeTypes_AddsAll()
    {
        var desc = new QueryDescription().WithAny<CombatState, Poisoned, Stunned>();
        Assert.Equal(3, desc.Any.Count);
    }

    [Fact]
    public void WithAny_FourTypes_AddsAll()
    {
        var desc = new QueryDescription().WithAny<CombatState, Poisoned, Stunned, Invisible>();
        Assert.Equal(4, desc.Any.Count);
    }

    [Fact]
    public void WithAny_DoesNotPollute_AllOrNoneLists()
    {
        var desc = new QueryDescription()
            .WithAll<Position>()
            .WithAny<CombatState, Poisoned>()
            .WithNone<Dead>();

        Assert.Single(desc.All);
        Assert.Equal(2, desc.Any.Count);
        Assert.Single(desc.None);
        Assert.DoesNotContain(typeof(CombatState), desc.All);
        Assert.DoesNotContain(typeof(CombatState), desc.None);
    }

    // =========================================================================
    // WithAny — Query<T1> (single component)
    // =========================================================================

    [Fact]
    public void QueryT1_WithAny_NoConstraint_AllPass()
    {
        // No WithAny = unconditional pass — regression guard.
        var world = NewWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);

        var seen = RunT1<Health>(world, new QueryDescription().WithAll<Health>());
        Assert.Equal(2, seen.Count);
    }

    [Fact]
    public void QueryT1_WithAny_EntityHasFirstType_Passes()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Set<CombatState>(e);   // satisfies WithAny

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState, Poisoned>();
        var seen = RunT1<Health>(world, desc);
        Assert.Single(seen);
        Assert.Contains(e, seen);
    }

    [Fact]
    public void QueryT1_WithAny_EntityHasSecondType_Passes()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Set<Poisoned>(e);      // satisfies WithAny via second type

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState, Poisoned>();
        var seen = RunT1<Health>(world, desc);
        Assert.Single(seen);
        Assert.Contains(e, seen);
    }

    [Fact]
    public void QueryT1_WithAny_EntityHasNeitherType_Excluded()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        // no CombatState, no Poisoned

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState, Poisoned>();
        var seen = RunT1<Health>(world, desc);
        Assert.Empty(seen);
    }

    [Fact]
    public void QueryT1_WithAny_OnlyEntityWithAnyTagIncluded()
    {
        var world = NewWorld();

        var inCombat = world.CreateEntity();
        world.Set<Health>(inCombat); world.Set<CombatState>(inCombat);

        var poisoned = world.CreateEntity();
        world.Set<Health>(poisoned); world.Set<Poisoned>(poisoned);

        var idle = world.CreateEntity();
        world.Set<Health>(idle);     // has neither tag

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState, Poisoned>();
        var seen = RunT1<Health>(world, desc);

        Assert.Equal(2, seen.Count);
        Assert.Contains(inCombat, seen);
        Assert.Contains(poisoned, seen);
        Assert.DoesNotContain(idle, seen);
    }

    [Fact]
    public void QueryT1_WithAny_EntityHasBothAnyTypes_VisitedOnce()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Set<CombatState>(e);
        world.Set<Poisoned>(e);   // has both — must still be visited exactly once

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState, Poisoned>();
        var seen = RunT1<Health>(world, desc);
        Assert.Single(seen);
    }

    [Fact]
    public void QueryT1_WithAny_UnregisteredType_NobodyHasIt_AllExcluded()
    {
        // If nobody has any of the WithAny types, all entities are excluded.
        var world = NewWorld();
        var e = world.CreateEntity(); world.Set<Health>(e);

        var desc = new QueryDescription().WithAll<Health>().WithAny<Invisible>();
        var seen = RunT1<Health>(world, desc);
        Assert.Empty(seen);
    }

    [Fact]
    public void QueryT1_WithAny_AnyTagAddedMidTick_EntityAppearsNextQuery()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState>();

        Assert.Empty(RunT1<Health>(world, desc));

        world.Set<CombatState>(e);
        Assert.Single(RunT1<Health>(world, desc));
    }

    [Fact]
    public void QueryT1_WithAny_AnyTagRemovedMidTick_EntityDisappears()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e); world.Set<CombatState>(e);
        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState>();

        Assert.Single(RunT1<Health>(world, desc));

        world.Remove<CombatState>(e);
        Assert.Empty(RunT1<Health>(world, desc));
    }

    // =========================================================================
    // WithAny — Query<T1,T2>
    // =========================================================================

    [Fact]
    public void QueryT2_WithAny_PassesWhenEntityHasAnyType()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e); world.Set<Mana>(e); world.Set<CombatState>(e);

        var desc = new QueryDescription().WithAll<Health, Mana>().WithAny<CombatState, Poisoned>();
        var seen = RunT2<Health, Mana>(world, desc);
        Assert.Single(seen);
        Assert.Contains(e, seen);
    }

    [Fact]
    public void QueryT2_WithAny_ExcludesWhenEntityHasNone()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e); world.Set<Mana>(e);
        // no CombatState, no Poisoned

        var desc = new QueryDescription().WithAll<Health, Mana>().WithAny<CombatState, Poisoned>();
        var seen = RunT2<Health, Mana>(world, desc);
        Assert.Empty(seen);
    }

    [Fact]
    public void QueryT2_WithAny_MixedEntities_OnlyMatchingReturned()
    {
        var world = NewWorld();

        var active = world.CreateEntity();
        world.Set<Health>(active); world.Set<Mana>(active); world.Set<CombatState>(active);

        var poisoned = world.CreateEntity();
        world.Set<Health>(poisoned); world.Set<Mana>(poisoned); world.Set<Poisoned>(poisoned);

        var idle = world.CreateEntity();
        world.Set<Health>(idle); world.Set<Mana>(idle);

        var desc = new QueryDescription().WithAll<Health, Mana>().WithAny<CombatState, Poisoned>();
        var seen = RunT2<Health, Mana>(world, desc);

        Assert.Equal(2, seen.Count);
        Assert.Contains(active, seen);
        Assert.Contains(poisoned, seen);
        Assert.DoesNotContain(idle, seen);
    }

    [Fact]
    public void QueryT2_WithAny_RefMutation_Persists()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 10 });
        world.Set(e, new Mana { Current = 5 });
        world.Set<CombatState>(e);

        var desc = new QueryDescription().WithAll<Health, Mana>().WithAny<CombatState>();
        world.Query<Health, Mana>(in desc,
            (EntityId _, ref Health hp, ref Mana mp) => { hp.Current = 99; mp.Current = 99; });

        Assert.Equal(99, world.Get<Health>(e).Current);
        Assert.Equal(99, world.Get<Mana>(e).Current);
    }

    // =========================================================================
    // WithAny — Query<T1,T2,T3>
    // =========================================================================

    [Fact]
    public void QueryT3_WithAny_PassesWhenOneAnyTypePresent()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e); world.Set<Mana>(e); world.Set<Position>(e);
        world.Set<Poisoned>(e);

        var desc = new QueryDescription()
            .WithAll<Health, Mana, Position>()
            .WithAny<CombatState, Poisoned>();

        Assert.Single(RunT3<Health, Mana, Position>(world, desc));
    }

    [Fact]
    public void QueryT3_WithAny_ExcludesWhenNoAnyTypePresent()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e); world.Set<Mana>(e); world.Set<Position>(e);
        // no CombatState, no Poisoned

        var desc = new QueryDescription()
            .WithAll<Health, Mana, Position>()
            .WithAny<CombatState, Poisoned>();

        Assert.Empty(RunT3<Health, Mana, Position>(world, desc));
    }

    // =========================================================================
    // WithAny — Query<T1,T2,T3,T4>
    // =========================================================================

    [Fact]
    public void QueryT4_WithAny_PassesWhenOneAnyTypePresent()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e); world.Set<Mana>(e);
        world.Set<Position>(e); world.Set<Velocity>(e);
        world.Set<CombatState>(e);

        var desc = new QueryDescription()
            .WithAll<Health, Mana, Position, Velocity>()
            .WithAny<CombatState, Poisoned>();

        Assert.Single(RunT4<Health, Mana, Position, Velocity>(world, desc));
    }

    [Fact]
    public void QueryT4_WithAny_ExcludesWhenNoAnyTypePresent()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e); world.Set<Mana>(e);
        world.Set<Position>(e); world.Set<Velocity>(e);

        var desc = new QueryDescription()
            .WithAll<Health, Mana, Position, Velocity>()
            .WithAny<CombatState, Poisoned>();

        Assert.Empty(RunT4<Health, Mana, Position, Velocity>(world, desc));
    }

    // =========================================================================
    // WithAny combined with WithNone — all arities
    // =========================================================================

    [Fact]
    public void QueryT1_WithAnyAndWithNone_BothFiltersApplied()
    {
        var world = NewWorld();

        // passes WithAny (has CombatState) + passes WithNone (no Dead) → included
        var ok = world.CreateEntity();
        world.Set<Health>(ok); world.Set<CombatState>(ok);

        // passes WithAny but fails WithNone (has Dead) → excluded
        var deadFighter = world.CreateEntity();
        world.Set<Health>(deadFighter); world.Set<CombatState>(deadFighter); world.Set<Dead>(deadFighter);

        // fails WithAny (no CombatState, no Poisoned) → excluded
        var idle = world.CreateEntity();
        world.Set<Health>(idle);

        var desc = new QueryDescription()
            .WithAll<Health>()
            .WithAny<CombatState, Poisoned>()
            .WithNone<Dead>();

        var seen = RunT1<Health>(world, desc);
        Assert.Single(seen);
        Assert.Contains(ok, seen);
    }

    [Fact]
    public void QueryT2_WithAnyAndWithNone_BothFiltersApplied()
    {
        var world = NewWorld();

        var ok = world.CreateEntity();
        world.Set<Health>(ok); world.Set<Mana>(ok); world.Set<CombatState>(ok);

        var dead = world.CreateEntity();
        world.Set<Health>(dead); world.Set<Mana>(dead);
        world.Set<CombatState>(dead); world.Set<Dead>(dead);

        var noAny = world.CreateEntity();
        world.Set<Health>(noAny); world.Set<Mana>(noAny);

        var desc = new QueryDescription()
            .WithAll<Health, Mana>()
            .WithAny<CombatState, Poisoned>()
            .WithNone<Dead>();

        var seen = RunT2<Health, Mana>(world, desc);
        Assert.Single(seen);
        Assert.Contains(ok, seen);
    }

    [Fact]
    public void QueryT3_WithAnyAndWithNone_BothFiltersApplied()
    {
        var world = NewWorld();

        var ok = world.CreateEntity();
        world.Set<Health>(ok); world.Set<Mana>(ok); world.Set<Position>(ok);
        world.Set<CombatState>(ok);

        var stunned = world.CreateEntity();
        world.Set<Health>(stunned); world.Set<Mana>(stunned); world.Set<Position>(stunned);
        world.Set<CombatState>(stunned); world.Set<Stunned>(stunned);

        var idle = world.CreateEntity();
        world.Set<Health>(idle); world.Set<Mana>(idle); world.Set<Position>(idle);

        var desc = new QueryDescription()
            .WithAll<Health, Mana, Position>()
            .WithAny<CombatState, Poisoned>()
            .WithNone<Stunned>();

        var seen = RunT3<Health, Mana, Position>(world, desc);
        Assert.Single(seen);
        Assert.Contains(ok, seen);
    }

    [Fact]
    public void QueryT4_WithAnyAndWithNone_BothFiltersApplied()
    {
        var world = NewWorld();

        var ok = world.CreateEntity();
        world.Set<Health>(ok); world.Set<Mana>(ok);
        world.Set<Position>(ok); world.Set<Velocity>(ok);
        world.Set<CombatState>(ok);

        var dead = world.CreateEntity();
        world.Set<Health>(dead); world.Set<Mana>(dead);
        world.Set<Position>(dead); world.Set<Velocity>(dead);
        world.Set<CombatState>(dead); world.Set<Dead>(dead);

        var desc = new QueryDescription()
            .WithAll<Health, Mana, Position, Velocity>()
            .WithAny<CombatState, Poisoned>()
            .WithNone<Dead>();

        var seen = RunT4<Health, Mana, Position, Velocity>(world, desc);
        Assert.Single(seen);
        Assert.Contains(ok, seen);
    }

    // =========================================================================
    // WithAny edge cases
    // =========================================================================

    [Fact]
    public void WithAny_SingleOption_ActsLikeRequiredWhenPresent()
    {
        // WithAny<T> with one type = entity must have T.
        var world = NewWorld();
        var hasIt = world.CreateEntity(); world.Set<Health>(hasIt); world.Set<CombatState>(hasIt);
        var lacksIt = world.CreateEntity(); world.Set<Health>(lacksIt);

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState>();
        var seen = RunT1<Health>(world, desc);

        Assert.Single(seen);
        Assert.Contains(hasIt, seen);
    }

    [Fact]
    public void WithAny_FourOptions_EntityWithAnyOnePasses()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Set<Invisible>(e);   // only the 4th option

        var desc = new QueryDescription()
            .WithAll<Health>()
            .WithAny<CombatState, Poisoned, Stunned, Invisible>();

        var seen = RunT1<Health>(world, desc);
        Assert.Single(seen);
        Assert.Contains(e, seen);
    }

    [Fact]
    public void WithAny_NoDuplicates_WhenMultipleAnyTypesSatisfied()
    {
        // Entity has all four WithAny types — should still appear exactly once.
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Set<CombatState>(e);
        world.Set<Poisoned>(e);
        world.Set<Stunned>(e);
        world.Set<Invisible>(e);

        var desc = new QueryDescription()
            .WithAll<Health>()
            .WithAny<CombatState, Poisoned, Stunned, Invisible>();

        var seen = RunT1<Health>(world, desc);
        Assert.Single(seen);
    }

    [Fact]
    public void WithAny_ReusedAcrossTicks_ReflectsCurrentState()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set<Health>(e);

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState, Poisoned>();

        // Tick 1 — no tag
        Assert.Empty(RunT1<Health>(world, desc));

        // Tick 2 — enters combat
        world.Set<CombatState>(e);
        Assert.Single(RunT1<Health>(world, desc));

        // Tick 3 — combat ends, gets poisoned
        world.Remove<CombatState>(e);
        world.Set<Poisoned>(e);
        Assert.Single(RunT1<Health>(world, desc));

        // Tick 4 — poison cured, idle again
        world.Remove<Poisoned>(e);
        Assert.Empty(RunT1<Health>(world, desc));
    }

    [Fact]
    public void WithAny_DestroyedEntity_NotReturned()
    {
        var world = NewWorld();
        var alive = world.CreateEntity();
        world.Set<Health>(alive); world.Set<CombatState>(alive);

        var dying = world.CreateEntity();
        world.Set<Health>(dying); world.Set<CombatState>(dying);
        world.DestroyEntity(dying);

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState>();
        var seen = RunT1<Health>(world, desc);

        Assert.Single(seen);
        Assert.Contains(alive, seen);
    }

    // =========================================================================
    // Regression: existing WithAll + WithNone behaviour unchanged after
    // WithAny addition.  Each test is a direct copy of a pre-WithAny test
    // with an *explicit* empty WithAny to confirm the no-constraint fast-path.
    // =========================================================================

    [Fact]
    public void Regression_WithAllOnly_UnaffectedByWithAnyAddition()
    {
        var world = NewWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);
        var e3 = world.CreateEntity(); // no Health

        // No WithAny specified → PassesAny returns true for everyone
        var seen = RunT1<Health>(world, new QueryDescription().WithAll<Health>());
        Assert.Equal(2, seen.Count);
        Assert.Contains(e1, seen);
        Assert.Contains(e2, seen);
    }

    [Fact]
    public void Regression_WithNone_StillWorksAlongsideWithAnyPresent()
    {
        // WithAny is specified; WithNone must still exclude correctly.
        var world = NewWorld();

        var ok = world.CreateEntity();
        world.Set<Health>(ok); world.Set<CombatState>(ok);

        var excluded = world.CreateEntity();
        world.Set<Health>(excluded); world.Set<CombatState>(excluded); world.Set<Dead>(excluded);

        var desc = new QueryDescription()
            .WithAll<Health>()
            .WithAny<CombatState>()
            .WithNone<Dead>();

        var seen = RunT1<Health>(world, desc);
        Assert.Single(seen);
        Assert.Contains(ok, seen);
    }

    [Fact]
    public void Regression_PivotLogic_StillCorrectWithWithAnyPresent_T2()
    {
        // Pivot correctness must hold even when WithAny is active.
        // s1 (Health) = 1 entity, s2 (Mana) = 100 entities.
        var world = NewWorld();

        var both = world.CreateEntity();
        world.Set<Health>(both); world.Set<Mana>(both); world.Set<CombatState>(both);

        for (int i = 0; i < 99; i++)
        {
            var e = world.CreateEntity();
            world.Set<Mana>(e); // Mana only — no Health
        }

        var desc = new QueryDescription()
            .WithAll<Health, Mana>()
            .WithAny<CombatState, Poisoned>();

        var seen = RunT2<Health, Mana>(world, desc);
        Assert.Single(seen);
        Assert.Contains(both, seen);
    }

    [Fact]
    public void Regression_RefMutation_StillPersistsWithWithAnyPresent()
    {
        var world = NewWorld();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 1 });
        world.Set<CombatState>(e);

        var desc = new QueryDescription().WithAll<Health>().WithAny<CombatState>();
        world.Query<Health>(in desc, (EntityId _, ref Health hp) => hp.Current = 42);

        Assert.Equal(42, world.Get<Health>(e).Current);
    }

    [Fact]
    public void Regression_NoDuplicates_WithWithAnyPresent()
    {
        var world = NewWorld();
        for (int i = 0; i < 30; i++)
        {
            var e = world.CreateEntity();
            world.Set<Health>(e);
            world.Set<Mana>(e);
            // Alternating between two WithAny types to exercise both branches
            if (i % 2 == 0) world.Set<CombatState>(e);
            else world.Set<Poisoned>(e);
        }

        var desc = new QueryDescription()
            .WithAll<Health, Mana>()
            .WithAny<CombatState, Poisoned>();

        var seen = RunT2<Health, Mana>(world, desc);
        Assert.Equal(30, seen.Count);
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }


    // Removing component while iterating
    [Fact]
    public void RemoveComponent_DuringQuery()
    {
        var world = NewWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);

        var desc = new QueryDescription()
            .WithAll<Health>();

        world.Query(in desc, (EntityId e,
            ref Health health) =>
        {
            world.Remove<Health>(e);
        });

        Assert.False(world.Has<Health>(e1));
        Assert.False(world.Has<Health>(e2));
    }

    // Removing component while iterating
    [Fact]
    public void RemoveComponent_DuringQuery2()
    {
        var world = NewWorld();
        var e1 = world.CreateEntity(); world.Set<Health>(e1);
        var e2 = world.CreateEntity(); world.Set<Health>(e2);

        var query = new Query(world)
            .With<Health>();
        foreach (var e in query)
        {
            world.Remove<Health>(e);
        }

        Assert.False(world.Has<Health>(e1));
        Assert.False(world.Has<Health>(e2));
    }


    // =========================================================================
    // Integration: MUD tick — WithAny in realistic scenarios
    // =========================================================================

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