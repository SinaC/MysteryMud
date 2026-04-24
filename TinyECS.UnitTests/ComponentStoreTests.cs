namespace TinyECS.UnitTests;

/// <summary>
/// Tests for ComponentStore&lt;T&gt; in isolation (no World).
/// We fabricate EntityIds directly since the constructor is internal — we access
/// it from the same assembly via InternalsVisibleTo, or we use the World as a
/// factory.  Here we just use the World to get valid IDs.
/// </summary>
public class ComponentStoreTests
{
    // Helpers
    private struct Position { public int X, Y; }
    private struct Tag { }          // zero-size tag component

    // Convenience: get a fresh store + a handful of valid EntityId IDs
    private static (ComponentStore<Position> store, EntityId[] ids) MakeStore(int count = 4)
    {
        var world = new World();
        var ids = new EntityId[count];
        for (int i = 0; i < count; i++) ids[i] = world.CreateEntity();
        return (world.Store<Position>(), ids);
    }

    // -------------------------------------------------------------------------
    // Initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void NewStore_IsEmpty()
    {
        var store = new ComponentStore<Position>();
        Assert.Equal(0, store.Count);
        Assert.Equal(0, store.Entities.Length);
        Assert.Equal(0, store.Components.Length);
    }

    // -------------------------------------------------------------------------
    // Set / Has
    // -------------------------------------------------------------------------

    [Fact]
    public void Set_IncreasesCount()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0], new Position { X = 1, Y = 2 });
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Has_ReturnsFalse_BeforeSet()
    {
        var (store, ids) = MakeStore();
        Assert.False(store.Has(ids[0]));
    }

    [Fact]
    public void Has_ReturnsTrue_AfterSet()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0]);
        Assert.True(store.Has(ids[0]));
    }

    [Fact]
    public void Set_Overwrites_ExistingValue()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0], new Position { X = 1, Y = 2 });
        store.Set(ids[0], new Position { X = 9, Y = 9 });

        Assert.Equal(1, store.Count);   // count must not increase
        Assert.Equal(9, store.Get(ids[0]).X);
        Assert.Equal(9, store.Get(ids[0]).Y);
    }

    [Fact]
    public void Set_ReturnsRef_ThatAliasesStoredValue()
    {
        var (store, ids) = MakeStore();
        ref Position pos = ref store.Set(ids[0], new Position { X = 1 });
        pos.X = 42;
        Assert.Equal(42, store.Get(ids[0]).X);
    }

    // -------------------------------------------------------------------------
    // Get
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ReturnsCorrectValue()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0], new Position { X = 3, Y = 7 });
        ref Position p = ref store.Get(ids[0]);
        Assert.Equal(3, p.X);
        Assert.Equal(7, p.Y);
    }

    [Fact]
    public void Get_ReturnsRef_MutationPersists()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0], new Position { X = 1 });
        store.Get(ids[0]).X = 99;
        Assert.Equal(99, store.Get(ids[0]).X);
    }

    [Fact]
    public void Get_Throws_WhenMissing()
    {
        var (store, ids) = MakeStore();
        Assert.Throws<InvalidOperationException>(() => store.Get(ids[0]));
    }

    // -------------------------------------------------------------------------
    // TryGet
    // -------------------------------------------------------------------------

    [Fact]
    public void TryGet_ReturnsTrue_WhenPresent()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0], new Position { X = 5 });
        bool found = store.TryGet(ids[0], out var val);
        Assert.True(found);
        Assert.Equal(5, val.X);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenAbsent()
    {
        var (store, ids) = MakeStore();
        bool found = store.TryGet(ids[0], out _);
        Assert.False(found);
    }

    // -------------------------------------------------------------------------
    // Remove
    // -------------------------------------------------------------------------

    [Fact]
    public void Remove_ReturnsFalse_WhenNotPresent()
    {
        var (store, ids) = MakeStore();
        Assert.False(store.Remove(ids[0]));
    }

    [Fact]
    public void Remove_ReturnsTrue_WhenPresent()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0]);
        Assert.True(store.Remove(ids[0]));
    }

    [Fact]
    public void Remove_DecrementsCount()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0]);
        store.Set(ids[1]);
        store.Remove(ids[0]);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Remove_Has_ReturnsFalse_AfterRemove()
    {
        var (store, ids) = MakeStore();
        store.Set(ids[0]);
        store.Remove(ids[0]);
        Assert.False(store.Has(ids[0]));
    }

    [Fact]
    public void Remove_LastElement_Works()
    {
        // Removing the element that happens to be last in the dense array
        // avoids the swap — this tests that code path.
        var (store, ids) = MakeStore();
        store.Set(ids[0], new Position { X = 1 });
        store.Set(ids[1], new Position { X = 2 });
        store.Remove(ids[1]);   // ids[1] is last

        Assert.Equal(1, store.Count);
        Assert.True(store.Has(ids[0]));
        Assert.False(store.Has(ids[1]));
    }

    [Fact]
    public void Remove_MiddleElement_SwapsWithLast_KeepsDensePackedCorrectly()
    {
        // After removing ids[0] (the first), ids[2] (which was last) should
        // swap into position 0 and still be findable.
        var (store, ids) = MakeStore(3);
        store.Set(ids[0], new Position { X = 10 });
        store.Set(ids[1], new Position { X = 20 });
        store.Set(ids[2], new Position { X = 30 });

        store.Remove(ids[0]);

        Assert.Equal(2, store.Count);
        Assert.False(store.Has(ids[0]));
        Assert.True(store.Has(ids[1]));
        Assert.True(store.Has(ids[2]));
        Assert.Equal(20, store.Get(ids[1]).X);
        Assert.Equal(30, store.Get(ids[2]).X);
    }

    [Fact]
    public void RemoveIfPresent_NoThrow_WhenMissing()
    {
        var (store, ids) = MakeStore();
        store.RemoveIfPresent(ids[0]);   // should not throw
        Assert.Equal(0, store.Count);
    }

    // -------------------------------------------------------------------------
    // Stale EntityId detection
    // -------------------------------------------------------------------------

    [Fact]
    public void Has_ReturnsFalse_ForStaleId()
    {
        var world = new World();
        var store = world.Store<Position>();
        var e = world.CreateEntity();
        store.Set(e);
        world.DestroyEntity(e);

        // e is now stale — Has must reject it
        Assert.False(store.Has(e));
    }

    [Fact]
    public void Get_Throws_ForStaleId()
    {
        var world = new World();
        var store = world.Store<Position>();
        var e = world.CreateEntity();
        store.Set(e, new Position { X = 1 });
        world.DestroyEntity(e);

        Assert.Throws<InvalidOperationException>(() => store.Get(e));
    }

    [Fact]
    public void RecycledSlot_NewEntity_DoesNotAliasOld()
    {
        var world = new World();
        var store = world.Store<Position>();

        var e1 = world.CreateEntity();
        store.Set(e1, new Position { X = 42 });
        world.DestroyEntity(e1);

        var e2 = world.CreateEntity();   // likely reuses e1's index
        Assert.False(store.Has(e2));     // e2 must not inherit e1's component
        Assert.False(store.Has(e1));     // e1 is still invalid
    }

    // -------------------------------------------------------------------------
    // Dense array growth
    // -------------------------------------------------------------------------

    [Fact]
    public void Store_GrowsBeyondInitialCapacity()
    {
        // Default dense capacity is 16 — add 100 entities to force growth.
        var world = new World();
        var store = world.Store<Position>();
        var ids = new EntityId[100];
        for (int i = 0; i < 100; i++)
        {
            ids[i] = world.CreateEntity();
            store.Set(ids[i], new Position { X = i });
        }

        Assert.Equal(100, store.Count);
        for (int i = 0; i < 100; i++)
            Assert.Equal(i, store.Get(ids[i]).X);
    }

    [Fact]
    public void Store_GrowsSparseArray_ForHighIndexEntities()
    {
        // Force sparse array to grow by having EntityId indices > 64.
        var world = new World();
        var store = world.Store<Position>();

        // Create 200 entities to push indices past the initial sparse size.
        var ids = new EntityId[200];
        for (int i = 0; i < 200; i++) ids[i] = world.CreateEntity();

        store.Set(ids[150], new Position { X = 150 });
        Assert.True(store.Has(ids[150]));
        Assert.Equal(150, store.Get(ids[150]).X);
    }

    // -------------------------------------------------------------------------
    // Iteration — Entities / Components spans
    // -------------------------------------------------------------------------

    [Fact]
    public void Entities_Span_ContainsAllAddedEntities()
    {
        var (store, ids) = MakeStore(3);
        store.Set(ids[0]); store.Set(ids[1]); store.Set(ids[2]);

        var entities = store.Entities.ToArray();
        Assert.Equal(3, entities.Length);
        Assert.Contains(ids[0], entities);
        Assert.Contains(ids[1], entities);
        Assert.Contains(ids[2], entities);
    }

    [Fact]
    public void Components_Span_ParallelToEntities()
    {
        var (store, ids) = MakeStore(2);
        store.Set(ids[0], new Position { X = 1 });
        store.Set(ids[1], new Position { X = 2 });

        var ents = store.Entities;
        var comps = store.Components;
        Assert.Equal(ents.Length, comps.Length);

        for (int i = 0; i < ents.Length; i++)
        {
            if (ents[i] == ids[0]) Assert.Equal(1, comps[i].X);
            if (ents[i] == ids[1]) Assert.Equal(2, comps[i].X);
        }
    }

    // -------------------------------------------------------------------------
    // Enumerator (Each())
    // -------------------------------------------------------------------------

    [Fact]
    public void Each_VisitsAllEntities()
    {
        var (store, ids) = MakeStore(3);
        store.Set(ids[0], new Position { X = 10 });
        store.Set(ids[1], new Position { X = 20 });
        store.Set(ids[2], new Position { X = 30 });

        var seen = new List<EntityId>();
        var it = store.Each();
        while (it.MoveNext()) seen.Add(it.Entity);

        Assert.Equal(3, seen.Count);
        Assert.Contains(ids[0], seen);
        Assert.Contains(ids[1], seen);
        Assert.Contains(ids[2], seen);
    }

    [Fact]
    public void Each_RefComponent_MutationPersists()
    {
        var (store, ids) = MakeStore(2);
        store.Set(ids[0], new Position { X = 1 });
        store.Set(ids[1], new Position { X = 2 });

        var it = store.Each();
        while (it.MoveNext())
            it.Component.X += 100;

        Assert.Equal(101, store.Get(ids[0]).X);
        Assert.Equal(102, store.Get(ids[1]).X);
    }

    [Fact]
    public void Each_EmptyStore_YieldsNothing()
    {
        var store = new ComponentStore<Position>();
        var it = store.Each();
        Assert.False(it.MoveNext());
    }

    // -------------------------------------------------------------------------
    // Tag component (zero-size struct)
    // -------------------------------------------------------------------------

    [Fact]
    public void TagComponent_SetHasRemove_Works()
    {
        var world = new World();
        var store = world.Store<Tag>();
        var e = world.CreateEntity();

        store.Set(e);
        Assert.True(store.Has(e));
        store.Remove(e);
        Assert.False(store.Has(e));
    }

    // -------------------------------------------------------------------------
    // Add / Remove interleaving
    // -------------------------------------------------------------------------

    [Fact]
    public void InterleaveAddRemove_MaintainsConsistency()
    {
        var world = new World();
        var store = world.Store<Position>();
        var ids = new EntityId[10];
        for (int i = 0; i < 10; i++) ids[i] = world.CreateEntity();

        // Add all
        for (int i = 0; i < 10; i++) store.Set(ids[i], new Position { X = i });

        // Remove odd-indexed
        for (int i = 1; i < 10; i += 2) store.Remove(ids[i]);

        Assert.Equal(5, store.Count);
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0)
            {
                Assert.True(store.Has(ids[i]));
                Assert.Equal(i, store.Get(ids[i]).X);
            }
            else
            {
                Assert.False(store.Has(ids[i]));
            }
        }
    }

    [Fact]
    public void AddAfterRemove_Works()
    {
        var (store, ids) = MakeStore(2);
        store.Set(ids[0], new Position { X = 1 });
        store.Remove(ids[0]);
        store.Set(ids[0], new Position { X = 99 });

        Assert.True(store.Has(ids[0]));
        Assert.Equal(99, store.Get(ids[0]).X);
    }
}
