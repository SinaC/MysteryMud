namespace TinyECS.UnitTests;

public class WorldTests
{
    private struct Health { public int Current, Max; }
    private struct Mana { public int Current; }
    private struct Tag { }

    // -------------------------------------------------------------------------
    // CreateEntity
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateEntity_ReturnsValidEntity()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.True(e.IsValid);
    }

    [Fact]
    public void CreateEntity_ReturnsUniqueIds()
    {
        var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void CreateEntity_IsAlive_AfterCreation()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.True(world.IsAlive(e));
    }

    [Fact]
    public void CreateEntity_GenerationStartsAt1()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.Equal(1u, e.Generation);
    }

    [Fact]
    public void CreateManyEntities_AllAlive()
    {
        var world = new World();
        var ids = Enumerable.Range(0, 200).Select(_ => world.CreateEntity()).ToList();
        Assert.All(ids, e => Assert.True(world.IsAlive(e)));
    }

    // -------------------------------------------------------------------------
    // DestroyEntity
    // -------------------------------------------------------------------------

    [Fact]
    public void DestroyEntity_IsAlive_ReturnsFalse()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.DestroyEntity(e);
        Assert.False(world.IsAlive(e));
    }

    [Fact]
    public void DestroyEntity_StaleHandle_IsNotAlive()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.DestroyEntity(e);
        Assert.False(world.IsAlive(e));   // old handle remains stale
    }

    [Fact]
    public void DestroyEntity_Throws_WhenAlreadyDead()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.DestroyEntity(e);
        Assert.Throws<InvalidOperationException>(() => world.DestroyEntity(e));
    }

    [Fact]
    public void DestroyEntity_Throws_ForInvalidEntity()
    {
        var world = new World();
        Assert.Throws<InvalidOperationException>(() => world.DestroyEntity(EntityId.Invalid));
    }

    [Fact]
    public void DestroyEntity_StripsAllComponents()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 10 });
        world.Set(e, new Mana { Current = 5 });
        world.Set<Tag>(e);

        world.DestroyEntity(e);

        // Even the stores must no longer report the component
        Assert.False(world.Store<Health>().Has(e));
        Assert.False(world.Store<Mana>().Has(e));
        Assert.False(world.Store<Tag>().Has(e));
    }

    // -------------------------------------------------------------------------
    // Slot recycling & generation
    // -------------------------------------------------------------------------

    [Fact]
    public void RecycledSlot_HasIncrementedGeneration()
    {
        var world = new World();
        var e1 = world.CreateEntity();
        uint firstGen = e1.Generation;

        world.DestroyEntity(e1);
        var e2 = world.CreateEntity();   // should reuse same slot

        Assert.Equal(e1.Index, e2.Index);
        Assert.Equal(firstGen + 1, e2.Generation);
    }

    [Fact]
    public void RecycledSlot_OldHandle_IsNotAlive()
    {
        var world = new World();
        var old = world.CreateEntity();
        world.DestroyEntity(old);
        var newer = world.CreateEntity(); // reuses index

        Assert.True(world.IsAlive(newer));
        Assert.False(world.IsAlive(old));
    }

    [Fact]
    public void FreedSlots_AreReused_InLIFOOrder()
    {
        var world = new World();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();

        world.DestroyEntity(e2);
        world.DestroyEntity(e1);

        var r1 = world.CreateEntity();  // should get e1's slot (top of stack)
        var r2 = world.CreateEntity();  // should get e2's slot

        Assert.Equal(e1.Index, r1.Index);
        Assert.Equal(e2.Index, r2.Index);
    }

    // -------------------------------------------------------------------------
    // IsAlive edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void IsAlive_InvalidEntity_ReturnsFalse()
    {
        var world = new World();
        Assert.False(world.IsAlive(EntityId.Invalid));
    }

    [Fact]
    public void IsAlive_EntityFromDifferentWorld_ReturnsFalse()
    {
        var world1 = new World();
        var world2 = new World();
        var e = world1.CreateEntity();
        // world2 has no slots yet — index is out of range
        Assert.False(world2.IsAlive(e));
    }

    // -------------------------------------------------------------------------
    // Set
    // -------------------------------------------------------------------------

    [Fact]
    public void Set_AddsComponent()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 50, Max = 100 });
        Assert.True(world.Has<Health>(e));
    }

    [Fact]
    public void Set_Throws_ForDeadEntity()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.DestroyEntity(e);
        Assert.Throws<InvalidOperationException>(() => world.Set<Health>(e));
    }

    [Fact]
    public void Set_ReturnsRef_AllowsInPlaceMutation()
    {
        var world = new World();
        var e = world.CreateEntity();
        ref Health h = ref world.Set<Health>(e);
        h.Current = 77;
        Assert.Equal(77, world.Get<Health>(e).Current);
    }

    // -------------------------------------------------------------------------
    // Has
    // -------------------------------------------------------------------------

    [Fact]
    public void Has_ReturnsFalse_BeforeSet()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.False(world.Has<Health>(e));
    }

    [Fact]
    public void Has_ReturnsFalse_AfterRemove()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Remove<Health>(e);
        Assert.False(world.Has<Health>(e));
    }

    // -------------------------------------------------------------------------
    // Get
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ReturnsRef_MutationPersists()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 10 });
        world.Get<Health>(e).Current = 99;
        Assert.Equal(99, world.Get<Health>(e).Current);
    }

    [Fact]
    public void Get_Throws_WhenComponentMissing()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.Throws<InvalidOperationException>(() => world.Get<Health>(e));
    }

    // -------------------------------------------------------------------------
    // TryGet
    // -------------------------------------------------------------------------

    [Fact]
    public void TryGet_ReturnsTrue_WhenPresent()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 30 });
        bool found = world.TryGet<Health>(e, out var h);
        Assert.True(found);
        Assert.Equal(30, h.Current);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenAbsent()
    {
        var world = new World();
        var e = world.CreateEntity();
        Assert.False(world.TryGet<Health>(e, out _));
    }

    // -------------------------------------------------------------------------
    // Remove
    // -------------------------------------------------------------------------

    [Fact]
    public void Remove_NoThrow_WhenComponentAbsent()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Remove<Health>(e);   // should be a no-op
    }

    [Fact]
    public void Remove_ThenSet_Works()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 1 });
        world.Remove<Health>(e);
        world.Set(e, new Health { Current = 2 });
        Assert.Equal(2, world.Get<Health>(e).Current);
    }

    // -------------------------------------------------------------------------
    // Store<T> lazily created
    // -------------------------------------------------------------------------

    [Fact]
    public void Store_LazilyCreated_AndStable()
    {
        var world = new World();
        var s1 = world.Store<Health>();
        var s2 = world.Store<Health>();
        Assert.Same(s1, s2);
    }

    [Fact]
    public void DifferentTypes_HaveSeparateStores()
    {
        var world = new World();
        // Cast to object to compare references; generics make them different types
        object s1 = world.Store<Health>();
        object s2 = world.Store<Mana>();
        Assert.NotSame(s1, s2);
    }

    // -------------------------------------------------------------------------
    // Multiple components on one entity
    // -------------------------------------------------------------------------

    [Fact]
    public void Entity_CanHold_MultipleComponentTypes()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set(e, new Health { Current = 10, Max = 100 });
        world.Set(e, new Mana { Current = 50 });
        world.Set<Tag>(e);

        Assert.True(world.Has<Health>(e));
        Assert.True(world.Has<Mana>(e));
        Assert.True(world.Has<Tag>(e));
        Assert.Equal(10, world.Get<Health>(e).Current);
        Assert.Equal(50, world.Get<Mana>(e).Current);
    }

    [Fact]
    public void RemovingOneComponent_DoesNotAffectOthers()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Set<Health>(e);
        world.Set<Mana>(e);

        world.Remove<Health>(e);

        Assert.False(world.Has<Health>(e));
        Assert.True(world.Has<Mana>(e));
    }

    // -------------------------------------------------------------------------
    // Large-scale stress
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateDestroyManyEntities_CorrectAliveCount()
    {
        var world = new World();
        const int N = 500;
        var ids = new EntityId[N];
        for (int i = 0; i < N; i++) ids[i] = world.CreateEntity();

        // Destroy every other one
        for (int i = 0; i < N; i += 2) world.DestroyEntity(ids[i]);

        int alive = 0;
        for (int i = 0; i < N; i++)
            if (world.IsAlive(ids[i])) alive++;

        Assert.Equal(N / 2, alive);
    }
}
