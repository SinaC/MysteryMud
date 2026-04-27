using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace TinyECS.Pool;


/// <summary>
/// Delegate-based Query overloads for <see cref="PoolWorld"/>.
///
/// Mirrors <see cref="WorldQueryExtensions"/> exactly, but calls
/// <see cref="ComponentPool{T}.GetAtIndex"/> instead of
/// <see cref="ComponentStore{T}.GetAtDenseIndex"/>.
///
/// The critical difference in the hot loop:
///
///   ComponentStore (sparse-set):
///     int pos = _sparse[entity.Index];   // random read → cache miss
///     ref T c = ref _components[pos];    // random read → cache miss
///
///   ComponentPool (flat pool):
///     ref T c = ref _pool[entity.Index]; // direct indexed read → likely L1/L2 hit
///
/// The entity index from the pivot's iteration list is used directly as the
/// pool slot address for all non-pivot components — zero sparse indirection.
/// </summary>
public static class WorldPoolQueryExtensions
{
    // =========================================================================
    // HasPredicate — zero-allocation filter wrapper  (same as WorldQueryExtensions)
    // =========================================================================

    internal readonly struct HasPredicate
    {
        private readonly PoolWorld.IComponentPool _pool;
        internal HasPredicate(PoolWorld.IComponentPool pool) => _pool = pool;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Has(EntityId e) => _pool.Has(e);
    }

    // =========================================================================
    // Query<T1>
    // =========================================================================

    public static void Query<T1>(
        this PoolWorld world,
        in QueryDescription desc,
        QueryCallback<T1> callback)
    {
        var p1 = world.Pool<T1>();
        EnsureCache(world, desc);
        var none = desc.CachedPoolNone!;
        var any = desc.CachedPoolAny!;

        for (int i = p1.Count - 1; i >= 0; i--)
        {
            EntityId e = p1.EntityAt(i);
            int ei = (int)e.Index;         // entity index IS the pool address
            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;
            callback(e, ref p1.GetAtIndex(ei));
        }
    }

    // =========================================================================
    // Query<T1, T2>
    // =========================================================================

    public static void Query<T1, T2>(
        this PoolWorld world,
        in QueryDescription desc,
        QueryCallback<T1, T2> callback)
    {
        var p1 = world.Pool<T1>();
        var p2 = world.Pool<T2>();
        EnsureCache(world, desc);
        var none = desc.CachedPoolNone!;
        var any = desc.CachedPoolAny!;

        bool pivotIs1 = p1.Count <= p2.Count;
        if (pivotIs1)
        {
            for (int i = p1.Count - 1; i >= 0; i--)
            {
                EntityId e = p1.EntityAt(i);
                int ei = (int)e.Index;
                int ei2 = p2.GetEntityIndexFast(e); if (ei2 < 0) continue;
                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;
                callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2));
            }
        }
        else
        {
            for (int i = p2.Count - 1; i >= 0; i--)
            {
                EntityId e = p2.EntityAt(i);
                int ei2 = (int)e.Index;
                int ei = p1.GetEntityIndexFast(e); if (ei < 0) continue;
                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;
                callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2));
            }
        }
    }

    // =========================================================================
    // Query<T1, T2, T3>
    // =========================================================================

    public static void Query<T1, T2, T3>(
        this PoolWorld world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3> callback)
    {
        var p1 = world.Pool<T1>();
        var p2 = world.Pool<T2>();
        var p3 = world.Pool<T3>();
        EnsureCache(world, desc);
        var none = desc.CachedPoolNone!;
        var any = desc.CachedPoolAny!;

        int pivotIdx = MinIndex(p1.Count, p2.Count, p3.Count);
        switch (pivotIdx)
        {
            case 0:
                for (int i = p1.Count - 1; i >= 0; i--)
                {
                    EntityId e = p1.EntityAt(i);
                    int ei = (int)e.Index;
                    int ei2 = p2.GetEntityIndexFast(e); if (ei2 < 0) continue;
                    int ei3 = p3.GetEntityIndexFast(e); if (ei3 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2), ref p3.GetAtIndex(ei3));
                }
                break;
            case 1:
                for (int i = p2.Count - 1; i >= 0; i--)
                {
                    EntityId e = p2.EntityAt(i);
                    int ei2 = (int)e.Index;
                    int ei = p1.GetEntityIndexFast(e); if (ei < 0) continue;
                    int ei3 = p3.GetEntityIndexFast(e); if (ei3 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2), ref p3.GetAtIndex(ei3));
                }
                break;
            default:
                for (int i = p3.Count - 1; i >= 0; i--)
                {
                    EntityId e = p3.EntityAt(i);
                    int ei3 = (int)e.Index;
                    int ei = p1.GetEntityIndexFast(e); if (ei < 0) continue;
                    int ei2 = p2.GetEntityIndexFast(e); if (ei2 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2), ref p3.GetAtIndex(ei3));
                }
                break;
        }
    }

    // =========================================================================
    // Query<T1, T2, T3, T4>
    // =========================================================================

    public static void Query<T1, T2, T3, T4>(
        this PoolWorld world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3, T4> callback)
    {
        var p1 = world.Pool<T1>();
        var p2 = world.Pool<T2>();
        var p3 = world.Pool<T3>();
        var p4 = world.Pool<T4>();
        EnsureCache(world, desc);
        var none = desc.CachedPoolNone!;
        var any = desc.CachedPoolAny!;

        int pivotIdx = MinIndex(p1.Count, p2.Count, p3.Count, p4.Count);
        switch (pivotIdx)
        {
            case 0:
                for (int i = p1.Count - 1; i >= 0; i--)
                {
                    EntityId e = p1.EntityAt(i);
                    int ei = (int)e.Index;
                    int ei2 = p2.GetEntityIndexFast(e); if (ei2 < 0) continue;
                    int ei3 = p3.GetEntityIndexFast(e); if (ei3 < 0) continue;
                    int ei4 = p4.GetEntityIndexFast(e); if (ei4 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2), ref p3.GetAtIndex(ei3), ref p4.GetAtIndex(ei4));
                }
                break;
            case 1:
                for (int i = p2.Count - 1; i >= 0; i--)
                {
                    EntityId e = p2.EntityAt(i);
                    int ei2 = (int)e.Index;
                    int ei = p1.GetEntityIndexFast(e); if (ei < 0) continue;
                    int ei3 = p3.GetEntityIndexFast(e); if (ei3 < 0) continue;
                    int ei4 = p4.GetEntityIndexFast(e); if (ei4 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2), ref p3.GetAtIndex(ei3), ref p4.GetAtIndex(ei4));
                }
                break;
            case 2:
                for (int i = p3.Count - 1; i >= 0; i--)
                {
                    EntityId e = p3.EntityAt(i);
                    int ei3 = (int)e.Index;
                    int ei = p1.GetEntityIndexFast(e); if (ei < 0) continue;
                    int ei2 = p2.GetEntityIndexFast(e); if (ei2 < 0) continue;
                    int ei4 = p4.GetEntityIndexFast(e); if (ei4 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2), ref p3.GetAtIndex(ei3), ref p4.GetAtIndex(ei4));
                }
                break;
            default:
                for (int i = p4.Count - 1; i >= 0; i--)
                {
                    EntityId e = p4.EntityAt(i);
                    int ei4 = (int)e.Index;
                    int ei = p1.GetEntityIndexFast(e); if (ei < 0) continue;
                    int ei2 = p2.GetEntityIndexFast(e); if (ei2 < 0) continue;
                    int ei3 = p3.GetEntityIndexFast(e); if (ei3 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref p1.GetAtIndex(ei), ref p2.GetAtIndex(ei2), ref p3.GetAtIndex(ei3), ref p4.GetAtIndex(ei4));
                }
                break;
        }
    }

    // =========================================================================
    // Cache management
    // =========================================================================

    private static void EnsureCache(PoolWorld world, QueryDescription desc)
    {
        if (ReferenceEquals(desc.CachedPoolWorld, world)) return;
        desc.CachedPoolNone = BuildPredicates(world, desc.None);
        desc.CachedPoolAny = BuildPredicates(world, desc.Any);
        desc.CachedPoolWorld = world;
    }

    private static HasPredicate[] BuildPredicates(PoolWorld world, List<Type> types)
    {
        if (types.Count == 0) return Array.Empty<HasPredicate>();
        var result = new HasPredicate[types.Count];
        for (int i = 0; i < types.Count; i++)
            result[i] = new HasPredicate(world.PoolByType(types[i]));
        return result;
    }

    // =========================================================================
    // Hot-loop filter helpers
    // =========================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AnyHas(HasPredicate[] predicates, EntityId entity)
    {
        for (int i = 0; i < predicates.Length; i++)
            if (predicates[i].Has(entity)) return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool PassesAny(HasPredicate[] predicates, EntityId entity)
    {
        if (predicates.Length == 0) return true;
        for (int i = 0; i < predicates.Length; i++)
            if (predicates[i].Has(entity)) return true;
        return false;
    }

    // =========================================================================
    // Pivot selection
    // =========================================================================

    private static int MinIndex(int a, int b, int c)
    {
        if (a <= b && a <= c) return 0;
        if (b <= c) return 1;
        return 2;
    }

    private static int MinIndex(int a, int b, int c, int d)
    {
        int idx3 = MinIndex(a, b, c);
        int val3 = idx3 switch { 0 => a, 1 => b, _ => c };
        return d < val3 ? 3 : idx3;
    }
}
