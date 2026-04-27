using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using TinyECS;
using TinyECS.Pool;

namespace MysteryMud.Benchmarks.ECS;
// =============================================================================
// Benchmark configuration
// =============================================================================

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]                   // reports Gen0/1/2 GC and allocated bytes
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class EcsBenchmarks
{
    // -------------------------------------------------------------------------
    // Parameters
    // -------------------------------------------------------------------------

    [Params(10_000, 100_000)]
    public int EntityCount { get; set; }

    [Params(10, 100, 500)]
    public int TickCount { get; set; }

    // 1 % of entities undergo structural churn each tick
    private const double ChurnFraction = 0.01;
    private const int CompsToRemove = 3;   // remove up to 3 components per churning entity
    private const int CompsToAdd = 3;   // add    up to 3 components per churning entity

    // -------------------------------------------------------------------------
    // State — custom sparse-set ECS
    // -------------------------------------------------------------------------

    private TinyECS.World _sparseWorld = null!;
    private TinyECS.EntityId[] _sparseEntities = null!;

    // Pre-built query descriptions (built once, reused every tick)
    private TinyECS.QueryDescription _sparseMovementDesc = null!;
    private TinyECS.QueryDescription _sparseCombatDesc = null!;

    // -------------------------------------------------------------------------
    // State — Arch (archetypal ECS)
    // -------------------------------------------------------------------------

    private Arch.Core.World _archWorld = null!;
    private Arch.Core.Entity[] _archEntities = null!;

    private Arch.Core.QueryDescription _archMovementDesc;
    private Arch.Core.QueryDescription _archCombatDesc;

    // -------------------------------------------------------------------------
    // State — DefaultEcs (sparse-set ECS)
    // -------------------------------------------------------------------------

    private DefaultEcs.World _defaultWorld = null!;
    private DefaultEcs.Entity[] _defaultEntities = null!;

    // DefaultEcs queries are EntitySet objects — built once, live-updated.
    // We use two sets: one for movement (Position+Velocity, no Stunned/Dead)
    // and one for combat (Health+CombatState, no Dead).
    private DefaultEcs.EntitySet _defaultMovementSet = null!;
    private DefaultEcs.EntitySet _defaultCombatSet = null!;

    // -------------------------------------------------------------------------
    // State — Pool (flat-pool ECS — our implementation variant)
    // -------------------------------------------------------------------------

    private PoolWorld _poolWorld = null!;
    private EntityId[] _poolEntities = null!;

    private QueryDescription _poolMovementDesc = null!;
    private QueryDescription _poolCombatDesc = null!;

    // -------------------------------------------------------------------------
    // Deterministic RNG shared across both setups so both worlds see the
    // same churn pattern in every iteration.
    // -------------------------------------------------------------------------

    private Random _rng = null!;

    // Indices of the 100 (= 1 %) entities that churn each tick — same list
    // for both ECS implementations so the comparison is apples-to-apples.
    private int[] _churnIndices = null!;

    // -------------------------------------------------------------------------
    // GlobalSetup — runs once before all benchmark iterations
    // -------------------------------------------------------------------------

    [GlobalSetup]
    public void Setup()
    {
        _rng = new Random(42);   // fixed seed → reproducible

        // Determine which entity indices churn each tick (same for both worlds)
        int churnCount = Math.Max(1, (int)(EntityCount * ChurnFraction));
        _churnIndices = Enumerable.Range(0, EntityCount)
                                   .OrderBy(_ => _rng.Next())
                                   .Take(churnCount)
                                   .ToArray();

        SetupSparse();
        SetupArch();
        SetupDefault();
        SetupPool();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Arch.Core.World.Destroy(_archWorld);
    }

    // =========================================================================
    // ── Benchmark 1: Setup cost ───────────────────────────────────────────────
    //
    // Measures how long it takes to create 10 000 entities and assign all
    // 30 components.  This is a pure structural-write benchmark.
    // =========================================================================

    [Benchmark(Description = "Sparse | Setup (create + assign 30 comps by entity)")]
    public void Sparse_Setup()
    {
        // Re-run the setup so BenchmarkDotNet can measure it in isolation.
        var world = new TinyECS.World();
        PopulateSparseWorld(world, EntityCount);
    }

    [Benchmark(Description = "Arch   | Setup (create + assign 30 comps by entity)")]
    public void Arch_Setup()
    {
        var world = Arch.Core.World.Create();
        PopulateArchWorld(world, EntityCount);
        Arch.Core.World.Destroy(world);
    }

    [Benchmark(Description = "Default| Setup (create + assign 30 comps by entity)")]
    public void Default_Setup()
    {
        using var world = new DefaultEcs.World();
        PopulateDefaultWorld(world, EntityCount);
    }

    [Benchmark(Description = "Pool   | Setup (create + assign 30 comps × 10k entities)")]
    public void Pool_Setup()
    {
        var world = new PoolWorld();
        PopulatePoolWorld(world, EntityCount);
    }

    // =========================================================================
    // ── Benchmark 2: Query-only (no churn) ───────────────────────────────────
    //
    // 10 ticks, each tick iterates two queries (movement + combat) and
    // mutates matched components.  No structural changes — pure read/write
    // throughput with all 10 000 entities stable.
    // =========================================================================

    [Benchmark(Description = "Sparse | query only (no churn)")]
    public void Sparse_QueryOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunSparseMovementQuery();
            RunSparseCombatQuery();
        }
    }

    [Benchmark(Description = "Arch   | query only (no churn)")]
    public void Arch_QueryOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunArchMovementQuery();
            RunArchCombatQuery();
        }
    }

    [Benchmark(Description = "Default| query only (no churn)")]
    public void Default_QueryOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunDefaultMovementQuery();
            RunDefaultCombatQuery();
        }
    }

    [Benchmark(Description = "Pool   | 10 ticks — query only (no churn)")]
    public void Pool_QueryOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunPoolMovementQuery();
            RunPoolCombatQuery();
        }
    }

    // =========================================================================
    // ── Benchmark 3: Churn-only (no queries) ─────────────────────────────────
    //
    // 10 ticks of pure structural mutation on 1 % of entities.
    // Isolates add/remove cost from iteration cost.
    // =========================================================================

    [Benchmark(Description = "Sparse | churn only (1% entities, ±3 comps)")]
    public void Sparse_ChurnOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
            ApplySparseChurn();
    }

    [Benchmark(Description = "Arch   | churn only (1% entities, ±3 comps)")]
    public void Arch_ChurnOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
            ApplyArchChurn();
    }

    [Benchmark(Description = "Default| churn only (1% entities, ±3 comps)")]
    public void Default_ChurnOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
            ApplyDefaultChurn();
    }

    [Benchmark(Description = "Pool   | 10 ticks — churn only (1% entities, ±3 comps)")]
    public void Pool_ChurnOnly()
    {
        for (int tick = 0; tick < TickCount; tick++)
            ApplyPoolChurn();
    }

    // =========================================================================
    // ── Benchmark 4: Full tick (query + churn) ────────────────────────────────
    //
    // The headline number.  Each of 10 ticks:
    //   1. Movement query  — Position + Velocity, mutate Position
    //   2. Combat  query   — Health + CombatState, mutate Health
    //   3. 1 % churn       — remove up to 3, add up to 3 per churning entity
    // =========================================================================

    [Benchmark(Description = "Sparse | full (queries + 1% churn)")]
    public void Sparse_FullTick()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunSparseMovementQuery();
            RunSparseCombatQuery();
            ApplySparseChurn();
        }
    }

    [Benchmark(Description = "Arch   | full (queries + 1% churn)")]
    public void Arch_FullTick()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunArchMovementQuery();
            RunArchCombatQuery();
            ApplyArchChurn();
        }
    }

    [Benchmark(Description = "Default| full (queries + 1% churn)")]
    public void Default_FullTick()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunDefaultMovementQuery();
            RunDefaultCombatQuery();
            ApplyDefaultChurn();
        }
    }

    [Benchmark(Description = "Pool   | 10 ticks — full (queries + 1% churn)")]
    public void Pool_FullTick()
    {
        for (int tick = 0; tick < TickCount; tick++)
        {
            RunPoolMovementQuery();
            RunPoolCombatQuery();
            ApplyPoolChurn();
        }
    }

    // =========================================================================
    // ── Benchmark 5: Churn-heavy (3× more churn) ─────────────────────────────
    //
    // Stress-tests structural mutation specifically — triples the number of
    // entities that churn per tick.  Highlights the cost difference between
    // O(1) sparse-set add/remove and archetype migration.
    // =========================================================================

    [Benchmark(Description = "Sparse | churn-heavy (3% entities, ±3 comps)")]
    public void Sparse_ChurnHeavy()
    {
        int heavyCount = Math.Max(1, (int)(EntityCount * ChurnFraction * 3));
        for (int tick = 0; tick < TickCount; tick++)
        {
            for (int i = 0; i < heavyCount; i++)
            {
                int idx = _churnIndices[i % _churnIndices.Length];
                ApplySparseChurnForEntity(idx);
            }
        }
    }

    [Benchmark(Description = "Arch   | churn-heavy (3% entities, ±3 comps)")]
    public void Arch_ChurnHeavy()
    {
        int heavyCount = Math.Max(1, (int)(EntityCount * ChurnFraction * 3));
        for (int tick = 0; tick < TickCount; tick++)
        {
            for (int i = 0; i < heavyCount; i++)
            {
                int idx = _churnIndices[i % _churnIndices.Length];
                ApplyArchChurnForEntity(idx);
            }
        }
    }

    [Benchmark(Description = "Default| churn-heavy (3% entities, ±3 comps)")]
    public void Default_ChurnHeavy()
    {
        int heavyCount = Math.Max(1, (int)(EntityCount * ChurnFraction * 3));
        for (int tick = 0; tick < TickCount; tick++)
        {
            for (int i = 0; i < heavyCount; i++)
            {
                int idx = _churnIndices[i % _churnIndices.Length];
                ApplyDefaultChurnForEntity(idx);
            }
        }
    }

    [Benchmark(Description = "Pool   | 10 ticks — churn-heavy (3% entities, ±3 comps)")]
    public void Pool_ChurnHeavy()
    {
        int heavyCount = Math.Max(1, (int)(EntityCount * ChurnFraction * 3));
        for (int tick = 0; tick < TickCount; tick++)
        {
            for (int i = 0; i < heavyCount; i++)
            {
                int idx = _churnIndices[i % _churnIndices.Length];
                ApplyPoolChurnForEntity(idx);
            }
        }
    }


    // =========================================================================
    // Setup helpers
    // =========================================================================

    private void SetupSparse()
    {
        _sparseWorld = new TinyECS.World();
        _sparseEntities = PopulateSparseWorld(_sparseWorld, EntityCount);

        _sparseMovementDesc = new TinyECS.QueryDescription()
            .WithAll<Position, Velocity>()
            .WithNone<Stunned, Dead>();

        _sparseCombatDesc = new TinyECS.QueryDescription()
            .WithAll<Health, CombatState>()
            .WithNone<Dead>();
    }

    private static TinyECS.EntityId[] PopulateSparseWorld(TinyECS.World world, int count)
    {
        var entities = new TinyECS.EntityId[count];
        var rng = new Random(0);

        for (int i = 0; i < count; i++)
        {
            var e = world.CreateEntity();
            entities[i] = e;
            AssignAllComponentsSparse(world, e, rng);
        }
        return entities;
    }

    /// <summary>
    /// Assigns all 30 components to an entity.  Data components get
    /// seeded values; tag components are set unconditionally.
    /// </summary>
    private static void AssignAllComponentsSparse(TinyECS.World w, TinyECS.EntityId e, Random rng)
    {
        w.Set(e, new Position { X = rng.NextSingle(), Y = rng.NextSingle(), Z = rng.NextSingle() });
        w.Set(e, new Velocity { Dx = rng.NextSingle(), Dy = rng.NextSingle(), Dz = rng.NextSingle() });
        w.Set(e, new Health { Current = 100, Max = 100 });
        w.Set(e, new Mana { Current = 80, Max = 80 });
        w.Set(e, new Stamina { Current = 60, Max = 60 });
        w.Set(e, new Level { Value = rng.Next(1, 100) });
        w.Set(e, new Experience { Points = rng.NextInt64(0, 100_000) });
        w.Set(e, new Armor { Rating = rng.Next(0, 50) });
        w.Set(e, new Damage { Min = 5, Max = 15 });
        w.Set(e, new Speed { Value = 1.0f + rng.NextSingle() });
        w.Set(e, new Gold { Amount = rng.Next(0, 500) });
        w.Set(e, new Weight { Value = rng.NextSingle() * 200f });
        w.Set(e, new Age { Ticks = rng.Next(0, 10_000) });
        w.Set(e, new RoomRef { Vnum = rng.Next(1, 9999) });
        w.Set(e, new TargetRef { EntityPacked = 0 });
        w.Set(e, new PoisonDebuff { DamagePerTick = 0, TicksRemaining = 0 });
        w.Set(e, new BlindDebuff { TicksRemaining = 0 });
        w.Set(e, new SilenceDebuff { TicksRemaining = 0 });
        w.Set(e, new HasteDebuff { TicksRemaining = 0 });
        w.Set(e, new RegenBuff { HpPerTick = 5, TicksRemaining = 10 });
        w.Set(e, new ShieldBuff { Absorb = 20, TicksRemaining = 5 });
        w.Set(e, new BerserkBuff { BonusDamage = 10, TicksRemaining = 3 });
        w.Set<CombatState>(e);
        w.Set<Dead>(e);
        w.Set<Stunned>(e);
        w.Set<Invisible>(e);
        w.Set<Sanctuary>(e);
        w.Set<Sleeping>(e);
        w.Set<Fleeing>(e);
        w.Set<PlayerTag>(e);
        w.Set<MobileTag>(e);
    }

    // ── Arch setup ────────────────────────────────────────────────────────────

    private void SetupArch()
    {
        _archWorld = Arch.Core.World.Create();
        _archEntities = PopulateArchWorld(_archWorld, EntityCount);

        // Arch QueryDescription uses component-type arrays
        _archMovementDesc = new Arch.Core.QueryDescription()
            .WithAll<Position, Velocity>()
            .WithNone<Stunned, Dead>();

        _archCombatDesc = new Arch.Core.QueryDescription()
            .WithAll<Health, CombatState>()
            .WithNone<Dead>();
    }

    private static Arch.Core.Entity[] PopulateArchWorld(Arch.Core.World world, int count)
    {
        var entities = new Arch.Core.Entity[count];
        var rng = new Random(0);

        for (int i = 0; i < count; i++)
        {
            // Arch requires all components at creation time for archetype placement.
            // We create with the full 30-component set.
            entities[i] = world.Create();
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Position { X = rng.NextSingle(), Y = rng.NextSingle(), Z = rng.NextSingle() } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Velocity { Dx = rng.NextSingle(), Dy = rng.NextSingle(), Dz = rng.NextSingle() });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Health { Current = 100, Max = 100 } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Mana { Current = 80, Max = 80 } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Stamina { Current = 60, Max = 60 } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Level { Value = rng.Next(1, 100) } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Experience { Points = rng.NextInt64(0, 100_000) } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Armor { Rating = rng.Next(0, 50) } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Damage { Min = 5, Max = 15 } );
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Speed { Value = 1.0f + rng.NextSingle() });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Gold { Amount = rng.Next(0, 500) });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Weight { Value = rng.NextSingle() * 200f });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Age { Ticks = rng.Next(0, 10_000) });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new RoomRef { Vnum = rng.Next(1, 9999) });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new TargetRef { EntityPacked = 0 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new PoisonDebuff { DamagePerTick = 0, TicksRemaining = 0 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new BlindDebuff { TicksRemaining = 0 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new SilenceDebuff { TicksRemaining = 0 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new HasteDebuff { TicksRemaining = 0 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new RegenBuff { HpPerTick = 5, TicksRemaining = 10 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new ShieldBuff { Absorb = 20, TicksRemaining = 5 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new BerserkBuff { BonusDamage = 10, TicksRemaining = 3 });
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new CombatState());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Dead());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Stunned());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Invisible());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Sanctuary());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Sleeping());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new Fleeing());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new PlayerTag());
            Arch.Core.Extensions.EntityExtensions.Add(entities[i], new MobileTag());
        }
        return entities;
    }

    // =========================================================================
    // Query runners — Sparse
    // =========================================================================

    private void RunSparseMovementQuery()
    {
        var queryDescr = new TinyECS.QueryDescription()
            .WithAll<Position, Velocity>();
        TinyECS.Extensions.WorldQueryExtensions.Query(_sparseWorld, queryDescr,
            static (TinyECS.EntityId _, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.Dx;
                pos.Y += vel.Dy;
                pos.Z += vel.Dz;
            });
    }

    private void RunSparseCombatQuery()
    {
        var queryDescr = new TinyECS.QueryDescription()
            .WithAll<Health, CombatState>();
        TinyECS.Extensions.WorldQueryExtensions.Query(_sparseWorld, queryDescr,
            static (TinyECS.EntityId _, ref Health hp, ref CombatState _cs) =>
            {
                hp.Current = Math.Max(0, hp.Current - 1);
            });
    }

    // =========================================================================
    // Query runners — Arch
    // =========================================================================

    private void RunArchMovementQuery()
    {
        _archWorld.Query(in _archMovementDesc, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.Dx;
            pos.Y += vel.Dy;
            pos.Z += vel.Dz;
        });
    }

    private void RunArchCombatQuery()
    {
        _archWorld.Query(in _archCombatDesc, (ref Health hp, ref CombatState _) =>
        {
            hp.Current = Math.Max(0, hp.Current - 1);
        });
    }

    // =========================================================================
    // Churn helpers — Sparse
    //
    // Per-tick: for each churning entity, remove up to 3 components (if present)
    // and add up to 3 components (if absent).
    //
    // Component selection is deterministic per entity index so both ECS
    // implementations see the exact same structural changes.
    //
    // We cycle through a fixed sequence of (remove-set, add-set) pairs rather
    // than calling Random inside the hot loop — this keeps the benchmark measuring
    // ECS cost, not RNG cost.
    // =========================================================================

    // Rotating sets of component indices to remove / add.
    // Index into the 30-component "churn palette" below.
    private static readonly int[][] ChurnRemoveSets =
    {
    new[] { 22, 24, 26 },  // CombatState, Stunned, Sanctuary
    new[] { 23, 25, 27 },  // Dead, Invisible, Sleeping
    new[] { 22, 23, 28 },  // CombatState, Dead, Fleeing
};

    private static readonly int[][] ChurnAddSets =
    {
    new[] { 22, 24, 26 },  // CombatState, Stunned, Sanctuary
    new[] { 23, 25, 27 },  // Dead, Invisible, Sleeping
    new[] { 22, 23, 28 },  // CombatState, Dead, Fleeing
};

    private void ApplySparseChurn()
    {
        for (int i = 0; i < _churnIndices.Length; i++)
            ApplySparseChurnForEntity(_churnIndices[i]);
    }

    private void ApplySparseChurnForEntity(int entityIdx)
    {
        var e = _sparseEntities[entityIdx];
        int set = entityIdx % ChurnRemoveSets.Length;

        // Remove (if present)
        foreach (int ci in ChurnRemoveSets[set])
            SparseRemoveByIndex(_sparseWorld, e, ci);

        // Add (if absent)
        foreach (int ci in ChurnAddSets[set])
            SparseAddByIndex(_sparseWorld, e, ci);
    }

    /// <summary>Maps a component-palette index to a concrete Remove call.</summary>
    private static void SparseRemoveByIndex(TinyECS.World w, TinyECS.EntityId e, int idx)
    {
        switch (idx)
        {
            case 0: w.Remove<Position>(e); break;
            case 1: w.Remove<Velocity>(e); break;
            case 2: w.Remove<Health>(e); break;
            case 3: w.Remove<Mana>(e); break;
            case 4: w.Remove<Stamina>(e); break;
            case 5: w.Remove<Level>(e); break;
            case 6: w.Remove<Experience>(e); break;
            case 7: w.Remove<Armor>(e); break;
            case 8: w.Remove<Damage>(e); break;
            case 9: w.Remove<Speed>(e); break;
            case 10: w.Remove<Gold>(e); break;
            case 11: w.Remove<Weight>(e); break;
            case 12: w.Remove<Age>(e); break;
            case 13: w.Remove<RoomRef>(e); break;
            case 14: w.Remove<TargetRef>(e); break;
            case 15: w.Remove<PoisonDebuff>(e); break;
            case 16: w.Remove<BlindDebuff>(e); break;
            case 17: w.Remove<SilenceDebuff>(e); break;
            case 18: w.Remove<HasteDebuff>(e); break;
            case 19: w.Remove<RegenBuff>(e); break;
            case 20: w.Remove<ShieldBuff>(e); break;
            case 21: w.Remove<BerserkBuff>(e); break;
            case 22: w.Remove<CombatState>(e); break;
            case 23: w.Remove<Dead>(e); break;
            case 24: w.Remove<Stunned>(e); break;
            case 25: w.Remove<Invisible>(e); break;
            case 26: w.Remove<Sanctuary>(e); break;
            case 27: w.Remove<Sleeping>(e); break;
            case 28: w.Remove<Fleeing>(e); break;
            case 29: w.Remove<PlayerTag>(e); break;
        }
    }

    private static void SparseAddByIndex(TinyECS.World w, TinyECS.EntityId e, int idx)
    {
        switch (idx)
        {
            case 0: if (!w.Has<Position>(e)) w.Set<Position>(e); break;
            case 1: if (!w.Has<Velocity>(e)) w.Set<Velocity>(e); break;
            case 2: if (!w.Has<Health>(e)) w.Set<Health>(e); break;
            case 3: if (!w.Has<Mana>(e)) w.Set<Mana>(e); break;
            case 4: if (!w.Has<Stamina>(e)) w.Set<Stamina>(e); break;
            case 5: if (!w.Has<Level>(e)) w.Set<Level>(e); break;
            case 6: if (!w.Has<Experience>(e)) w.Set<Experience>(e); break;
            case 7: if (!w.Has<Armor>(e)) w.Set<Armor>(e); break;
            case 8: if (!w.Has<Damage>(e)) w.Set<Damage>(e); break;
            case 9: if (!w.Has<Speed>(e)) w.Set<Speed>(e); break;
            case 10: if (!w.Has<Gold>(e)) w.Set<Gold>(e); break;
            case 11: if (!w.Has<Weight>(e)) w.Set<Weight>(e); break;
            case 12: if (!w.Has<Age>(e)) w.Set<Age>(e); break;
            case 13: if (!w.Has<RoomRef>(e)) w.Set<RoomRef>(e); break;
            case 14: if (!w.Has<TargetRef>(e)) w.Set<TargetRef>(e); break;
            case 15: if (!w.Has<PoisonDebuff>(e)) w.Set<PoisonDebuff>(e); break;
            case 16: if (!w.Has<BlindDebuff>(e)) w.Set<BlindDebuff>(e); break;
            case 17: if (!w.Has<SilenceDebuff>(e)) w.Set<SilenceDebuff>(e); break;
            case 18: if (!w.Has<HasteDebuff>(e)) w.Set<HasteDebuff>(e); break;
            case 19: if (!w.Has<RegenBuff>(e)) w.Set<RegenBuff>(e); break;
            case 20: if (!w.Has<ShieldBuff>(e)) w.Set<ShieldBuff>(e); break;
            case 21: if (!w.Has<BerserkBuff>(e)) w.Set<BerserkBuff>(e); break;
            case 22: if (!w.Has<CombatState>(e)) w.Set<CombatState>(e); break;
            case 23: if (!w.Has<Dead>(e)) w.Set<Dead>(e); break;
            case 24: if (!w.Has<Stunned>(e)) w.Set<Stunned>(e); break;
            case 25: if (!w.Has<Invisible>(e)) w.Set<Invisible>(e); break;
            case 26: if (!w.Has<Sanctuary>(e)) w.Set<Sanctuary>(e); break;
            case 27: if (!w.Has<Sleeping>(e)) w.Set<Sleeping>(e); break;
            case 28: if (!w.Has<Fleeing>(e)) w.Set<Fleeing>(e); break;
            case 29: if (!w.Has<PlayerTag>(e)) w.Set<PlayerTag>(e); break;
        }
    }

    // =========================================================================
    // DefaultEcs setup
    // =========================================================================

    private void SetupDefault()
    {
        _defaultWorld = new DefaultEcs.World();
        _defaultEntities = PopulateDefaultWorld(_defaultWorld, EntityCount);

        // EntitySet is a live filtered view — built once, automatically kept
        // in sync as components are added/removed.  This matches how real code
        // would use DefaultEcs: build sets at startup, iterate every tick.
        //
        // Movement query: Position + Velocity, exclude Stunned + Dead
        _defaultMovementSet = _defaultWorld
            .GetEntities()
            .With<Position>()
            .With<Velocity>()
            .Without<Stunned>()
            .Without<Dead>()
            .AsSet();

        // Combat query: Health + CombatState, exclude Dead
        _defaultCombatSet = _defaultWorld
            .GetEntities()
            .With<Health>()
            .With<CombatState>()
            .Without<Dead>()
            .AsSet();
    }

    private static DefaultEcs.Entity[] PopulateDefaultWorld(
        DefaultEcs.World world, int count)
    {
        var entities = new DefaultEcs.Entity[count];
        var rng = new Random(0);   // same seed as other setups
        for (int i = 0; i < count; i++)
        {
            entities[i] = world.CreateEntity();
            DefaultEcsHelpers.AssignAll(entities[i], rng);
        }
        return entities;
    }

    // =========================================================================
    // Query runners — DefaultEcs
    //
    // DefaultEcs does not have a built-in multi-component ref-callback API like
    // Arch or our custom ECS.  The idiomatic approach is to iterate the EntitySet
    // and call entity.Get<T>() for each component.  Get<T>() returns a ref to
    // the internally stored value (no copy) so mutations persist.
    // =========================================================================

    private void RunDefaultMovementQuery()
    {
        // Iterate a ReadOnlySpan<Entity> — the EntitySet exposes its backing
        // span directly, so no enumerator allocation occurs.
        ReadOnlySpan<DefaultEcs.Entity> entities = _defaultMovementSet.GetEntities();
        for (int i = 0; i < entities.Length; i++)
        {
            ref Position pos = ref entities[i].Get<Position>();
            ref Velocity vel = ref entities[i].Get<Velocity>();
            pos.X += vel.Dx;
            pos.Y += vel.Dy;
            pos.Z += vel.Dz;
        }
    }

    private void RunDefaultCombatQuery()
    {
        ReadOnlySpan<DefaultEcs.Entity> entities = _defaultCombatSet.GetEntities();
        for (int i = 0; i < entities.Length; i++)
        {
            ref Health hp = ref entities[i].Get<Health>();
            hp.Current = Math.Max(0, hp.Current - 1);
        }
    }

    // =========================================================================
    // Churn helpers — DefaultEcs
    // =========================================================================

    private void ApplyDefaultChurn()
    {
        for (int i = 0; i < _churnIndices.Length; i++)
            ApplyDefaultChurnForEntity(_churnIndices[i]);
    }

    private void ApplyDefaultChurnForEntity(int entityIdx)
    {
        var e = _defaultEntities[entityIdx];
        int set = entityIdx % ChurnRemoveSets.Length;

        foreach (int ci in ChurnRemoveSets[set])
            DefaultEcsHelpers.RemoveByIndex(e, ci);

        foreach (int ci in ChurnAddSets[set])
            DefaultEcsHelpers.AddByIndex(e, ci);
    }


    // =========================================================================
    // Churn helpers — Arch
    // =========================================================================

    private void ApplyArchChurn()
    {
        for (int i = 0; i < _churnIndices.Length; i++)
            ApplyArchChurnForEntity(_churnIndices[i]);
    }

    private void ApplyArchChurnForEntity(int entityIdx)
    {
        var e = _archEntities[entityIdx];
        int set = entityIdx % ChurnRemoveSets.Length;

        foreach (int ci in ChurnRemoveSets[set])
            ArchRemoveByIndex(_archWorld, e, ci);

        foreach (int ci in ChurnAddSets[set])
            ArchAddByIndex(_archWorld, e, ci);
    }

    private static void ArchRemoveByIndex(Arch.Core.World w, Arch.Core.Entity e, int idx)
    {
        switch (idx)
        {
            // Arch throws if the component is absent, so guard with Has first.
            case 22: if (w.Has<CombatState>(e)) w.Remove<CombatState>(e); break;
            case 23: if (w.Has<Dead>(e)) w.Remove<Dead>(e); break;
            case 24: if (w.Has<Stunned>(e)) w.Remove<Stunned>(e); break;
            case 25: if (w.Has<Invisible>(e)) w.Remove<Invisible>(e); break;
            case 26: if (w.Has<Sanctuary>(e)) w.Remove<Sanctuary>(e); break;
            case 27: if (w.Has<Sleeping>(e)) w.Remove<Sleeping>(e); break;
            case 28: if (w.Has<Fleeing>(e)) w.Remove<Fleeing>(e); break;
            case 29: if (w.Has<PlayerTag>(e)) w.Remove<PlayerTag>(e); break;
            // Data components: same pattern
            case 0: if (w.Has<Position>(e)) w.Remove<Position>(e); break;
            case 1: if (w.Has<Velocity>(e)) w.Remove<Velocity>(e); break;
            case 2: if (w.Has<Health>(e)) w.Remove<Health>(e); break;
            case 3: if (w.Has<Mana>(e)) w.Remove<Mana>(e); break;
            case 4: if (w.Has<Stamina>(e)) w.Remove<Stamina>(e); break;
            case 5: if (w.Has<Level>(e)) w.Remove<Level>(e); break;
            case 6: if (w.Has<Experience>(e)) w.Remove<Experience>(e); break;
            case 7: if (w.Has<Armor>(e)) w.Remove<Armor>(e); break;
            case 8: if (w.Has<Damage>(e)) w.Remove<Damage>(e); break;
            case 9: if (w.Has<Speed>(e)) w.Remove<Speed>(e); break;
            case 10: if (w.Has<Gold>(e)) w.Remove<Gold>(e); break;
            case 11: if (w.Has<Weight>(e)) w.Remove<Weight>(e); break;
            case 12: if (w.Has<Age>(e)) w.Remove<Age>(e); break;
            case 13: if (w.Has<RoomRef>(e)) w.Remove<RoomRef>(e); break;
            case 14: if (w.Has<TargetRef>(e)) w.Remove<TargetRef>(e); break;
            case 15: if (w.Has<PoisonDebuff>(e)) w.Remove<PoisonDebuff>(e); break;
            case 16: if (w.Has<BlindDebuff>(e)) w.Remove<BlindDebuff>(e); break;
            case 17: if (w.Has<SilenceDebuff>(e)) w.Remove<SilenceDebuff>(e); break;
            case 18: if (w.Has<HasteDebuff>(e)) w.Remove<HasteDebuff>(e); break;
            case 19: if (w.Has<RegenBuff>(e)) w.Remove<RegenBuff>(e); break;
            case 20: if (w.Has<ShieldBuff>(e)) w.Remove<ShieldBuff>(e); break;
            case 21: if (w.Has<BerserkBuff>(e)) w.Remove<BerserkBuff>(e); break;
        }
    }

    private static void ArchAddByIndex(Arch.Core.World w, Arch.Core.Entity e, int idx)
    {
        switch (idx)
        {
            case 22: if (!w.Has<CombatState>(e)) w.Add<CombatState>(e); break;
            case 23: if (!w.Has<Dead>(e)) w.Add<Dead>(e); break;
            case 24: if (!w.Has<Stunned>(e)) w.Add<Stunned>(e); break;
            case 25: if (!w.Has<Invisible>(e)) w.Add<Invisible>(e); break;
            case 26: if (!w.Has<Sanctuary>(e)) w.Add<Sanctuary>(e); break;
            case 27: if (!w.Has<Sleeping>(e)) w.Add<Sleeping>(e); break;
            case 28: if (!w.Has<Fleeing>(e)) w.Add<Fleeing>(e); break;
            case 29: if (!w.Has<PlayerTag>(e)) w.Add<PlayerTag>(e); break;
            case 0: if (!w.Has<Position>(e)) w.Add(e, new Position()); break;
            case 1: if (!w.Has<Velocity>(e)) w.Add(e, new Velocity()); break;
            case 2: if (!w.Has<Health>(e)) w.Add(e, new Health { Current = 100, Max = 100 }); break;
            case 3: if (!w.Has<Mana>(e)) w.Add(e, new Mana { Current = 80, Max = 80 }); break;
            case 4: if (!w.Has<Stamina>(e)) w.Add(e, new Stamina { Current = 60, Max = 60 }); break;
            case 5: if (!w.Has<Level>(e)) w.Add(e, new Level { Value = 1 }); break;
            case 6: if (!w.Has<Experience>(e)) w.Add(e, new Experience()); break;
            case 7: if (!w.Has<Armor>(e)) w.Add(e, new Armor()); break;
            case 8: if (!w.Has<Damage>(e)) w.Add(e, new Damage { Min = 5, Max = 15 }); break;
            case 9: if (!w.Has<Speed>(e)) w.Add(e, new Speed { Value = 1f }); break;
            case 10: if (!w.Has<Gold>(e)) w.Add(e, new Gold()); break;
            case 11: if (!w.Has<Weight>(e)) w.Add(e, new Weight()); break;
            case 12: if (!w.Has<Age>(e)) w.Add(e, new Age()); break;
            case 13: if (!w.Has<RoomRef>(e)) w.Add(e, new RoomRef()); break;
            case 14: if (!w.Has<TargetRef>(e)) w.Add(e, new TargetRef()); break;
            case 15: if (!w.Has<PoisonDebuff>(e)) w.Add(e, new PoisonDebuff()); break;
            case 16: if (!w.Has<BlindDebuff>(e)) w.Add(e, new BlindDebuff()); break;
            case 17: if (!w.Has<SilenceDebuff>(e)) w.Add(e, new SilenceDebuff()); break;
            case 18: if (!w.Has<HasteDebuff>(e)) w.Add(e, new HasteDebuff()); break;
            case 19: if (!w.Has<RegenBuff>(e)) w.Add(e, new RegenBuff { HpPerTick = 5, TicksRemaining = 10 }); break;
            case 20: if (!w.Has<ShieldBuff>(e)) w.Add(e, new ShieldBuff { Absorb = 20, TicksRemaining = 5 }); break;
            case 21: if (!w.Has<BerserkBuff>(e)) w.Add(e, new BerserkBuff { BonusDamage = 10, TicksRemaining = 3 }); break;
        }
    }

    // =========================================================================
    // Pool setup
    // =========================================================================

    private void SetupPool()
    {
        _poolWorld = new PoolWorld();
        _poolEntities = PopulatePoolWorld(_poolWorld, EntityCount);

        // QueryDescription is shared between Sparse and Pool — each caches
        // its own predicate arrays in separate fields on QueryDescription.
        _poolMovementDesc = new QueryDescription()
            .WithAll<Position, Velocity>()
            .WithNone<Stunned, Dead>();

        _poolCombatDesc = new QueryDescription()
            .WithAll<Health, CombatState>()
            .WithNone<Dead>();
    }

    private static EntityId[] PopulatePoolWorld(PoolWorld world, int count)
    {
        var entities = new EntityId[count];
        var rng = new Random(0);
        for (int i = 0; i < count; i++)
        {
            var e = entities[i] = world.CreateEntity();
            world.Set(e, new Position { X = rng.NextSingle(), Y = rng.NextSingle(), Z = rng.NextSingle() });
            world.Set(e, new Velocity { Dx = rng.NextSingle(), Dy = rng.NextSingle(), Dz = rng.NextSingle() });
            world.Set(e, new Health { Current = 100, Max = 100 });
            world.Set(e, new Mana { Current = 80, Max = 80 });
            world.Set(e, new Stamina { Current = 60, Max = 60 });
            world.Set(e, new Level { Value = rng.Next(1, 100) });
            world.Set(e, new Experience { Points = rng.NextInt64(0, 100_000) });
            world.Set(e, new Armor { Rating = rng.Next(0, 50) });
            world.Set(e, new Damage { Min = 5, Max = 15 });
            world.Set(e, new Speed { Value = 1.0f + rng.NextSingle() });
            world.Set(e, new Gold { Amount = rng.Next(0, 500) });
            world.Set(e, new Weight { Value = rng.NextSingle() * 200f });
            world.Set(e, new Age { Ticks = rng.Next(0, 10_000) });
            world.Set(e, new RoomRef { Vnum = rng.Next(1, 9999) });
            world.Set(e, new TargetRef { EntityPacked = 0 });
            world.Set(e, new PoisonDebuff { DamagePerTick = 0, TicksRemaining = 0 });
            world.Set(e, new BlindDebuff { TicksRemaining = 0 });
            world.Set(e, new SilenceDebuff { TicksRemaining = 0 });
            world.Set(e, new HasteDebuff { TicksRemaining = 0 });
            world.Set(e, new RegenBuff { HpPerTick = 5, TicksRemaining = 10 });
            world.Set(e, new ShieldBuff { Absorb = 20, TicksRemaining = 5 });
            world.Set(e, new BerserkBuff { BonusDamage = 10, TicksRemaining = 3 });
            world.Set<CombatState>(e);
            world.Set<Dead>(e);
            world.Set<Stunned>(e);
            world.Set<Invisible>(e);
            world.Set<Sanctuary>(e);
            world.Set<Sleeping>(e);
            world.Set<Fleeing>(e);
            world.Set<PlayerTag>(e);
            world.Set<MobileTag>(e);
        }
        return entities;
    }

    // =========================================================================
    // Query runners — Pool
    // =========================================================================

    private void RunPoolMovementQuery()
    {
        _poolWorld.Query<Position, Velocity>(
            in _poolMovementDesc,
            static (EntityId _, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.Dx;
                pos.Y += vel.Dy;
                pos.Z += vel.Dz;
            });
    }

    private void RunPoolCombatQuery()
    {
        _poolWorld.Query<Health, CombatState>(
            in _poolCombatDesc,
            static (EntityId _, ref Health hp, ref CombatState _cs) =>
            {
                hp.Current = Math.Max(0, hp.Current - 1);
            });
    }

    // =========================================================================
    // Churn helpers — Pool
    // =========================================================================

    private void ApplyPoolChurn()
    {
        for (int i = 0; i < _churnIndices.Length; i++)
            ApplyPoolChurnForEntity(_churnIndices[i]);
    }

    private void ApplyPoolChurnForEntity(int entityIdx)
    {
        var e = _poolEntities[entityIdx];
        int set = entityIdx % ChurnRemoveSets.Length;

        foreach (int ci in ChurnRemoveSets[set])
            PoolRemoveByIndex(_poolWorld, e, ci);

        foreach (int ci in ChurnAddSets[set])
            PoolAddByIndex(_poolWorld, e, ci);
    }

    private static void PoolRemoveByIndex(PoolWorld w, EntityId e, int idx)
    {
        switch (idx)
        {
            case 0: w.Remove<Position>(e); break;
            case 1: w.Remove<Velocity>(e); break;
            case 2: w.Remove<Health>(e); break;
            case 3: w.Remove<Mana>(e); break;
            case 4: w.Remove<Stamina>(e); break;
            case 5: w.Remove<Level>(e); break;
            case 6: w.Remove<Experience>(e); break;
            case 7: w.Remove<Armor>(e); break;
            case 8: w.Remove<Damage>(e); break;
            case 9: w.Remove<Speed>(e); break;
            case 10: w.Remove<Gold>(e); break;
            case 11: w.Remove<Weight>(e); break;
            case 12: w.Remove<Age>(e); break;
            case 13: w.Remove<RoomRef>(e); break;
            case 14: w.Remove<TargetRef>(e); break;
            case 15: w.Remove<PoisonDebuff>(e); break;
            case 16: w.Remove<BlindDebuff>(e); break;
            case 17: w.Remove<SilenceDebuff>(e); break;
            case 18: w.Remove<HasteDebuff>(e); break;
            case 19: w.Remove<RegenBuff>(e); break;
            case 20: w.Remove<ShieldBuff>(e); break;
            case 21: w.Remove<BerserkBuff>(e); break;
            case 22: w.Remove<CombatState>(e); break;
            case 23: w.Remove<Dead>(e); break;
            case 24: w.Remove<Stunned>(e); break;
            case 25: w.Remove<Invisible>(e); break;
            case 26: w.Remove<Sanctuary>(e); break;
            case 27: w.Remove<Sleeping>(e); break;
            case 28: w.Remove<Fleeing>(e); break;
            case 29: w.Remove<PlayerTag>(e); break;
        }
    }

    private static void PoolAddByIndex(PoolWorld w, EntityId e, int idx)
    {
        switch (idx)
        {
            case 0: if (!w.Has<Position>(e)) w.Set<Position>(e); break;
            case 1: if (!w.Has<Velocity>(e)) w.Set<Velocity>(e); break;
            case 2: if (!w.Has<Health>(e)) w.Set<Health>(e); break;
            case 3: if (!w.Has<Mana>(e)) w.Set<Mana>(e); break;
            case 4: if (!w.Has<Stamina>(e)) w.Set<Stamina>(e); break;
            case 5: if (!w.Has<Level>(e)) w.Set<Level>(e); break;
            case 6: if (!w.Has<Experience>(e)) w.Set<Experience>(e); break;
            case 7: if (!w.Has<Armor>(e)) w.Set<Armor>(e); break;
            case 8: if (!w.Has<Damage>(e)) w.Set<Damage>(e); break;
            case 9: if (!w.Has<Speed>(e)) w.Set<Speed>(e); break;
            case 10: if (!w.Has<Gold>(e)) w.Set<Gold>(e); break;
            case 11: if (!w.Has<Weight>(e)) w.Set<Weight>(e); break;
            case 12: if (!w.Has<Age>(e)) w.Set<Age>(e); break;
            case 13: if (!w.Has<RoomRef>(e)) w.Set<RoomRef>(e); break;
            case 14: if (!w.Has<TargetRef>(e)) w.Set<TargetRef>(e); break;
            case 15: if (!w.Has<PoisonDebuff>(e)) w.Set<PoisonDebuff>(e); break;
            case 16: if (!w.Has<BlindDebuff>(e)) w.Set<BlindDebuff>(e); break;
            case 17: if (!w.Has<SilenceDebuff>(e)) w.Set<SilenceDebuff>(e); break;
            case 18: if (!w.Has<HasteDebuff>(e)) w.Set<HasteDebuff>(e); break;
            case 19: if (!w.Has<RegenBuff>(e)) w.Set<RegenBuff>(e); break;
            case 20: if (!w.Has<ShieldBuff>(e)) w.Set<ShieldBuff>(e); break;
            case 21: if (!w.Has<BerserkBuff>(e)) w.Set<BerserkBuff>(e); break;
            case 22: if (!w.Has<CombatState>(e)) w.Set<CombatState>(e); break;
            case 23: if (!w.Has<Dead>(e)) w.Set<Dead>(e); break;
            case 24: if (!w.Has<Stunned>(e)) w.Set<Stunned>(e); break;
            case 25: if (!w.Has<Invisible>(e)) w.Set<Invisible>(e); break;
            case 26: if (!w.Has<Sanctuary>(e)) w.Set<Sanctuary>(e); break;
            case 27: if (!w.Has<Sleeping>(e)) w.Set<Sleeping>(e); break;
            case 28: if (!w.Has<Fleeing>(e)) w.Set<Fleeing>(e); break;
            case 29: if (!w.Has<PlayerTag>(e)) w.Set<PlayerTag>(e); break;
        }
    }
}

// =============================================================================
// DefaultEcs helpers (outside the benchmark class for clarity)
// =============================================================================

public static class DefaultEcsHelpers
{
    // Assign all 30 components to a DefaultEcs entity
    public static void AssignAll(DefaultEcs.Entity e, Random rng)
    {
        e.Set(new Position { X = rng.NextSingle(), Y = rng.NextSingle(), Z = rng.NextSingle() });
        e.Set(new Velocity { Dx = rng.NextSingle(), Dy = rng.NextSingle(), Dz = rng.NextSingle() });
        e.Set(new Health { Current = 100, Max = 100 });
        e.Set(new Mana { Current = 80, Max = 80 });
        e.Set(new Stamina { Current = 60, Max = 60 });
        e.Set(new Level { Value = rng.Next(1, 100) });
        e.Set(new Experience { Points = rng.NextInt64(0, 100_000) });
        e.Set(new Armor { Rating = rng.Next(0, 50) });
        e.Set(new Damage { Min = 5, Max = 15 });
        e.Set(new Speed { Value = 1.0f + rng.NextSingle() });
        e.Set(new Gold { Amount = rng.Next(0, 500) });
        e.Set(new Weight { Value = rng.NextSingle() * 200f });
        e.Set(new Age { Ticks = rng.Next(0, 10_000) });
        e.Set(new RoomRef { Vnum = rng.Next(1, 9999) });
        e.Set(new TargetRef { EntityPacked = 0 });
        e.Set(new PoisonDebuff { DamagePerTick = 0, TicksRemaining = 0 });
        e.Set(new BlindDebuff { TicksRemaining = 0 });
        e.Set(new SilenceDebuff { TicksRemaining = 0 });
        e.Set(new HasteDebuff { TicksRemaining = 0 });
        e.Set(new RegenBuff { HpPerTick = 5, TicksRemaining = 10 });
        e.Set(new ShieldBuff { Absorb = 20, TicksRemaining = 5 });
        e.Set(new BerserkBuff { BonusDamage = 10, TicksRemaining = 3 });
        e.Set(new CombatState());
        e.Set(new Dead());
        e.Set(new Stunned());
        e.Set(new Invisible());
        e.Set(new Sanctuary());
        e.Set(new Sleeping());
        e.Set(new Fleeing());
        e.Set(new PlayerTag());
        // Note: only 30 components total; MobileTag omitted to keep count exact
    }

    public static void RemoveByIndex(DefaultEcs.Entity e, int idx)
    {
        // Remove only if present — DefaultEcs throws if component is absent
        switch (idx)
        {
            case 0: if (e.Has<Position>()) e.Remove<Position>(); break;
            case 1: if (e.Has<Velocity>()) e.Remove<Velocity>(); break;
            case 2: if (e.Has<Health>()) e.Remove<Health>(); break;
            case 3: if (e.Has<Mana>()) e.Remove<Mana>(); break;
            case 4: if (e.Has<Stamina>()) e.Remove<Stamina>(); break;
            case 5: if (e.Has<Level>()) e.Remove<Level>(); break;
            case 6: if (e.Has<Experience>()) e.Remove<Experience>(); break;
            case 7: if (e.Has<Armor>()) e.Remove<Armor>(); break;
            case 8: if (e.Has<Damage>()) e.Remove<Damage>(); break;
            case 9: if (e.Has<Speed>()) e.Remove<Speed>(); break;
            case 10: if (e.Has<Gold>()) e.Remove<Gold>(); break;
            case 11: if (e.Has<Weight>()) e.Remove<Weight>(); break;
            case 12: if (e.Has<Age>()) e.Remove<Age>(); break;
            case 13: if (e.Has<RoomRef>()) e.Remove<RoomRef>(); break;
            case 14: if (e.Has<TargetRef>()) e.Remove<TargetRef>(); break;
            case 15: if (e.Has<PoisonDebuff>()) e.Remove<PoisonDebuff>(); break;
            case 16: if (e.Has<BlindDebuff>()) e.Remove<BlindDebuff>(); break;
            case 17: if (e.Has<SilenceDebuff>()) e.Remove<SilenceDebuff>(); break;
            case 18: if (e.Has<HasteDebuff>()) e.Remove<HasteDebuff>(); break;
            case 19: if (e.Has<RegenBuff>()) e.Remove<RegenBuff>(); break;
            case 20: if (e.Has<ShieldBuff>()) e.Remove<ShieldBuff>(); break;
            case 21: if (e.Has<BerserkBuff>()) e.Remove<BerserkBuff>(); break;
            case 22: if (e.Has<CombatState>()) e.Remove<CombatState>(); break;
            case 23: if (e.Has<Dead>()) e.Remove<Dead>(); break;
            case 24: if (e.Has<Stunned>()) e.Remove<Stunned>(); break;
            case 25: if (e.Has<Invisible>()) e.Remove<Invisible>(); break;
            case 26: if (e.Has<Sanctuary>()) e.Remove<Sanctuary>(); break;
            case 27: if (e.Has<Sleeping>()) e.Remove<Sleeping>(); break;
            case 28: if (e.Has<Fleeing>()) e.Remove<Fleeing>(); break;
            case 29: if (e.Has<PlayerTag>()) e.Remove<PlayerTag>(); break;
        }
    }

    public static void AddByIndex(DefaultEcs.Entity e, int idx)
    {
        switch (idx)
        {
            case 0: if (!e.Has<Position>()) e.Set(new Position()); break;
            case 1: if (!e.Has<Velocity>()) e.Set(new Velocity()); break;
            case 2: if (!e.Has<Health>()) e.Set(new Health { Current = 100, Max = 100 }); break;
            case 3: if (!e.Has<Mana>()) e.Set(new Mana { Current = 80, Max = 80 }); break;
            case 4: if (!e.Has<Stamina>()) e.Set(new Stamina { Current = 60, Max = 60 }); break;
            case 5: if (!e.Has<Level>()) e.Set(new Level { Value = 1 }); break;
            case 6: if (!e.Has<Experience>()) e.Set(new Experience()); break;
            case 7: if (!e.Has<Armor>()) e.Set(new Armor()); break;
            case 8: if (!e.Has<Damage>()) e.Set(new Damage { Min = 5, Max = 15 }); break;
            case 9: if (!e.Has<Speed>()) e.Set(new Speed { Value = 1f }); break;
            case 10: if (!e.Has<Gold>()) e.Set(new Gold()); break;
            case 11: if (!e.Has<Weight>()) e.Set(new Weight()); break;
            case 12: if (!e.Has<Age>()) e.Set(new Age()); break;
            case 13: if (!e.Has<RoomRef>()) e.Set(new RoomRef()); break;
            case 14: if (!e.Has<TargetRef>()) e.Set(new TargetRef()); break;
            case 15: if (!e.Has<PoisonDebuff>()) e.Set(new PoisonDebuff()); break;
            case 16: if (!e.Has<BlindDebuff>()) e.Set(new BlindDebuff()); break;
            case 17: if (!e.Has<SilenceDebuff>()) e.Set(new SilenceDebuff()); break;
            case 18: if (!e.Has<HasteDebuff>()) e.Set(new HasteDebuff()); break;
            case 19: if (!e.Has<RegenBuff>()) e.Set(new RegenBuff { HpPerTick = 5, TicksRemaining = 10 }); break;
            case 20: if (!e.Has<ShieldBuff>()) e.Set(new ShieldBuff { Absorb = 20, TicksRemaining = 5 }); break;
            case 21: if (!e.Has<BerserkBuff>()) e.Set(new BerserkBuff { BonusDamage = 10, TicksRemaining = 3 }); break;
            case 22: if (!e.Has<CombatState>()) e.Set(new CombatState()); break;
            case 23: if (!e.Has<Dead>()) e.Set(new Dead()); break;
            case 24: if (!e.Has<Stunned>()) e.Set(new Stunned()); break;
            case 25: if (!e.Has<Invisible>()) e.Set(new Invisible()); break;
            case 26: if (!e.Has<Sanctuary>()) e.Set(new Sanctuary()); break;
            case 27: if (!e.Has<Sleeping>()) e.Set(new Sleeping()); break;
            case 28: if (!e.Has<Fleeing>()) e.Set(new Fleeing()); break;
            case 29: if (!e.Has<PlayerTag>()) e.Set(new PlayerTag()); break;
        }
    }
}


// =============================================================================
// BenchmarkDotNet configuration
// =============================================================================

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(3)
            .WithIterationCount(10)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));

        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
