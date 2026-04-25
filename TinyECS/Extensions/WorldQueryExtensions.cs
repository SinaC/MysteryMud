using System.Runtime.CompilerServices;

namespace TinyECS.Extensions;

using System.Runtime.CompilerServices;

/// <summary>
/// Delegate-based Query overloads for <see cref="World"/>.
///
/// STRUCTURAL MUTATION DURING ITERATION
/// ─────────────────────────────────────
/// All Query overloads here iterate the pivot store's dense array BACKWARDS
/// (high index → 0).  This makes Remove-during-iteration safe:
///
///   ComponentStore.Remove uses swap-with-last.  When iterating forward,
///   removing the entity at index i swaps the last entity into slot i,
///   and the forward loop then skips it (i++ jumps past it).
///
///   Iterating backward avoids this: when we remove at index i, the entity
///   that was at _count-1 moves into slot i.  Because i-- takes us to i-1
///   next, we will visit the swapped-in entity on a subsequent iteration.
///   All indices above i have already been visited so nothing is skipped.
///
/// CONTRACT: entities added during iteration are NOT visited (the loop
/// bound is fixed at entry).  This matches the standard ECS convention —
/// use a deferred add-list if you need to visit newly-added entities.
///
/// PERFORMANCE
/// ───────────
/// • Filter predicates cached on QueryDescription as HasPredicate[] —
///   zero allocation after the first call per world/desc pair.
/// • HasPredicate is a value-type wrapper; avoids interface vtable dispatch
///   in the hot loop.
/// • GetDenseIndexFast skips the generation re-check for non-pivot stores
///   (the entity came from a live pivot store, so it is implicitly live).
/// • Pivot is always the smallest required store, minimising iterations.
/// </summary>
public static class WorldQueryExtensions
{
    // =========================================================================
    // HasPredicate — zero-allocation filter wrapper
    // =========================================================================

    internal readonly struct HasPredicate
    {
        private readonly World.IComponentStore _store;
        internal HasPredicate(World.IComponentStore store) => _store = store;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Has(EntityId e) => _store.Has(e);
    }

    // =========================================================================
    // Query<T1>
    // =========================================================================

    public static void Query<T1>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1> callback)
    {
        var s1 = world.Store<T1>();
        EnsureCache(world, desc);
        var none = desc.CachedNone!;
        var any = desc.CachedAny!;

        // Iterate backwards — safe for Remove-during-callback.
        // Loop bound is captured once; newly added entities are not visited.
        for (int i = s1.Count - 1; i >= 0; i--)
        {
            EntityId e = s1.EntityAt(i);
            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;
            callback(e, ref s1.GetAtDenseIndex(i));
        }
    }

    // =========================================================================
    // Query<T1, T2>
    // =========================================================================

    public static void Query<T1, T2>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        EnsureCache(world, desc);
        var none = desc.CachedNone!;
        var any = desc.CachedAny!;

        // Lockstep fast-path removed: equal store counts does NOT imply equal
        // entity sets — e.g. s1=[A,B], s2=[A,C] both have count 2 but only A
        // has both components.  Zipping by index would silently pair wrong entities.

        bool pivotIs1 = s1.Count <= s2.Count;
        if (pivotIs1)
        {
            for (int i = s1.Count - 1; i >= 0; i--)
            {
                EntityId e = s1.EntityAt(i);
                int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;
                callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2));
            }
        }
        else
        {
            for (int i = s2.Count - 1; i >= 0; i--)
            {
                EntityId e = s2.EntityAt(i);
                int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;
                callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i));
            }
        }
    }

    // =========================================================================
    // Query<T1, T2, T3>
    // =========================================================================

    public static void Query<T1, T2, T3>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        EnsureCache(world, desc);
        var none = desc.CachedNone!;
        var any = desc.CachedAny!;

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count);
        switch (pivotIdx)
        {
            case 0:
                for (int i = s1.Count - 1; i >= 0; i--)
                {
                    EntityId e = s1.EntityAt(i);
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3));
                }
                break;
            case 1:
                for (int i = s2.Count - 1; i >= 0; i--)
                {
                    EntityId e = s2.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3));
                }
                break;
            default:
                for (int i = s3.Count - 1; i >= 0; i--)
                {
                    EntityId e = s3.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i));
                }
                break;
        }
    }

    // =========================================================================
    // Query<T1, T2, T3, T4>
    // =========================================================================

    public static void Query<T1, T2, T3, T4>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3, T4> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var s4 = world.Store<T4>();
        EnsureCache(world, desc);
        var none = desc.CachedNone!;
        var any = desc.CachedAny!;

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count);
        switch (pivotIdx)
        {
            case 0:
                for (int i = s1.Count - 1; i >= 0; i--)
                {
                    EntityId e = s1.EntityAt(i);
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
                }
                break;
            case 1:
                for (int i = s2.Count - 1; i >= 0; i--)
                {
                    EntityId e = s2.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
                }
                break;
            case 2:
                for (int i = s3.Count - 1; i >= 0; i--)
                {
                    EntityId e = s3.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i), ref s4.GetAtDenseIndex(p4));
                }
                break;
            default:
                for (int i = s4.Count - 1; i >= 0; i--)
                {
                    EntityId e = s4.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(i));
                }
                break;
        }
    }

    // =========================================================================
    // Query<T1, T2, T3, T4, T5>
    // =========================================================================

    public static void Query<T1, T2, T3, T4, T5>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3, T4, T5> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var s4 = world.Store<T4>();
        var s5 = world.Store<T5>();
        EnsureCache(world, desc);
        var none = desc.CachedNone!;
        var any = desc.CachedAny!;

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count, s5.Count);
        switch (pivotIdx)
        {
            case 0:
                for (int i = s1.Count - 1; i >= 0; i--)
                {
                    EntityId e = s1.EntityAt(i);
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
                    int p5 = s5.GetDenseIndexFast(e); if (p5 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4), ref s5.GetAtDenseIndex(p5));
                }
                break;
            case 1:
                for (int i = s2.Count - 1; i >= 0; i--)
                {
                    EntityId e = s2.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
                    int p5 = s5.GetDenseIndexFast(e); if (p5 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4), ref s5.GetAtDenseIndex(p5));
                }
                break;
            case 2:
                for (int i = s3.Count - 1; i >= 0; i--)
                {
                    EntityId e = s3.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
                    int p5 = s5.GetDenseIndexFast(e); if (p5 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i), ref s4.GetAtDenseIndex(p4), ref s5.GetAtDenseIndex(p5));
                }
                break;
            case 3:
                for (int i = s4.Count - 1; i >= 0; i--)
                {
                    EntityId e = s4.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    int p5 = s5.GetDenseIndexFast(e); if (p5 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(i), ref s5.GetAtDenseIndex(p5));
                }
                break;
            default:
                for (int i = s5.Count - 1; i >= 0; i--)
                {
                    EntityId e = s5.EntityAt(i);
                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
                    if (AnyHas(none, e)) continue;
                    if (!PassesAny(any, e)) continue;
                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4), ref s5.GetAtDenseIndex(i));
                }
                break;
        }
    }

    // =========================================================================
    // Cache management
    // =========================================================================

    private static void EnsureCache(World world, QueryDescription desc)
    {
        if (ReferenceEquals(desc.CachedWorld, world)) return;
        desc.CachedNone = BuildPredicates(world, desc.None);
        desc.CachedAny = BuildPredicates(world, desc.Any);
        desc.CachedWorld = world;
    }

    private static HasPredicate[] BuildPredicates(World world, List<Type> types)
    {
        if (types.Count == 0) return Array.Empty<HasPredicate>();
        var result = new HasPredicate[types.Count];
        for (int i = 0; i < types.Count; i++)
            result[i] = new HasPredicate(world.StoreByType(types[i]));
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

    private static int MinIndex(int a, int b, int c, int d, int e)
    {
        int idx4 = MinIndex(a, b, c, d);
        int val4 = idx4 switch { 0 => a, 1 => b, 2 => c, _ => d };
        return e < val4 ? 4 : idx4;
    }
}

///// <summary>
///// Delegate-based Query overloads for <see cref="World"/>.
/////
///// STRUCTURAL MUTATION DURING ITERATION
///// ─────────────────────────────────────
///// All Query overloads here iterate the pivot store's dense array BACKWARDS
///// (high index → 0).  This makes Remove-during-iteration safe:
/////
/////   ComponentStore.Remove uses swap-with-last.  When iterating forward,
/////   removing the entity at index i swaps the last entity into slot i,
/////   and the forward loop then skips it (i++ jumps past it).
/////
/////   Iterating backward avoids this: when we remove at index i, the entity
/////   that was at _count-1 moves into slot i.  Because i-- takes us to i-1
/////   next, we will visit the swapped-in entity on a subsequent iteration.
/////   All indices above i have already been visited so nothing is skipped.
/////
///// CONTRACT: entities added during iteration are NOT visited (the loop
///// bound is fixed at entry).  This matches the standard ECS convention —
///// use a deferred add-list if you need to visit newly-added entities.
/////
///// PERFORMANCE
///// ───────────
///// • Filter predicates cached on QueryDescription as HasPredicate[] —
/////   zero allocation after the first call per world/desc pair.
///// • HasPredicate is a value-type wrapper; avoids interface vtable dispatch
/////   in the hot loop.
///// • Lockstep fast-path: when all required stores have equal count and no
/////   filters are active, all arrays are zipped by index — zero sparse lookups.
///// • GetDenseIndexFast skips the generation re-check for non-pivot stores
/////   (the entity came from a live pivot store, so it is implicitly live).
///// </summary>
//public static class WorldQueryExtensions
//{
//    // =========================================================================
//    // HasPredicate — zero-allocation filter wrapper
//    // =========================================================================

//    internal readonly struct HasPredicate
//    {
//        private readonly World.IComponentStore _store;
//        internal HasPredicate(World.IComponentStore store) => _store = store;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        internal bool Has(EntityId e) => _store.Has(e);
//    }

//    // =========================================================================
//    // Query<T1>
//    // =========================================================================

//    public static void Query<T1>(
//        this World world,
//        in QueryDescription desc,
//        QueryCallback<T1> callback)
//    {
//        var s1 = world.Store<T1>();
//        EnsureCache(world, desc);
//        var none = desc.CachedNone!;
//        var any = desc.CachedAny!;

//        // Iterate backwards — safe for Remove-during-callback.
//        // Loop bound is captured once; newly added entities are not visited.
//        for (int i = s1.Count - 1; i >= 0; i--)
//        {
//            EntityId e = s1.EntityAt(i);
//            if (AnyHas(none, e)) continue;
//            if (!PassesAny(any, e)) continue;
//            callback(e, ref s1.GetAtDenseIndex(i));
//        }
//    }

//    // =========================================================================
//    // Query<T1, T2>
//    // =========================================================================

//    public static void Query<T1, T2>(
//        this World world,
//        in QueryDescription desc,
//        QueryCallback<T1, T2> callback)
//    {
//        var s1 = world.Store<T1>();
//        var s2 = world.Store<T2>();
//        EnsureCache(world, desc);
//        var none = desc.CachedNone!;
//        var any = desc.CachedAny!;

//        // Lockstep fast-path: equal counts + no filters → zip by index.
//        if (s1.Count == s2.Count && none.Length == 0 && any.Length == 0)
//        {
//            for (int i = s1.Count - 1; i >= 0; i--)
//                callback(s1.EntityAt(i),
//                    ref s1.GetAtDenseIndex(i),
//                    ref s2.GetAtDenseIndex(i));
//            return;
//        }

//        bool pivotIs1 = s1.Count <= s2.Count;
//        if (pivotIs1)
//        {
//            for (int i = s1.Count - 1; i >= 0; i--)
//            {
//                EntityId e = s1.EntityAt(i);
//                int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
//                if (AnyHas(none, e)) continue;
//                if (!PassesAny(any, e)) continue;
//                callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2));
//            }
//        }
//        else
//        {
//            for (int i = s2.Count - 1; i >= 0; i--)
//            {
//                EntityId e = s2.EntityAt(i);
//                int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
//                if (AnyHas(none, e)) continue;
//                if (!PassesAny(any, e)) continue;
//                callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i));
//            }
//        }
//    }

//    // =========================================================================
//    // Query<T1, T2, T3>
//    // =========================================================================

//    public static void Query<T1, T2, T3>(
//        this World world,
//        in QueryDescription desc,
//        QueryCallback<T1, T2, T3> callback)
//    {
//        var s1 = world.Store<T1>();
//        var s2 = world.Store<T2>();
//        var s3 = world.Store<T3>();
//        EnsureCache(world, desc);
//        var none = desc.CachedNone!;
//        var any = desc.CachedAny!;

//        if (s1.Count == s2.Count && s2.Count == s3.Count
//            && none.Length == 0 && any.Length == 0)
//        {
//            for (int i = s1.Count - 1; i >= 0; i--)
//                callback(s1.EntityAt(i),
//                    ref s1.GetAtDenseIndex(i),
//                    ref s2.GetAtDenseIndex(i),
//                    ref s3.GetAtDenseIndex(i));
//            return;
//        }

//        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count);
//        switch (pivotIdx)
//        {
//            case 0:
//                for (int i = s1.Count - 1; i >= 0; i--)
//                {
//                    EntityId e = s1.EntityAt(i);
//                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
//                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
//                    if (AnyHas(none, e)) continue;
//                    if (!PassesAny(any, e)) continue;
//                    callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3));
//                }
//                break;
//            case 1:
//                for (int i = s2.Count - 1; i >= 0; i--)
//                {
//                    EntityId e = s2.EntityAt(i);
//                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
//                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
//                    if (AnyHas(none, e)) continue;
//                    if (!PassesAny(any, e)) continue;
//                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3));
//                }
//                break;
//            default:
//                for (int i = s3.Count - 1; i >= 0; i--)
//                {
//                    EntityId e = s3.EntityAt(i);
//                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
//                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
//                    if (AnyHas(none, e)) continue;
//                    if (!PassesAny(any, e)) continue;
//                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i));
//                }
//                break;
//        }
//    }

//    // =========================================================================
//    // Query<T1, T2, T3, T4>
//    // =========================================================================

//    public static void Query<T1, T2, T3, T4>(
//        this World world,
//        in QueryDescription desc,
//        QueryCallback<T1, T2, T3, T4> callback)
//    {
//        var s1 = world.Store<T1>();
//        var s2 = world.Store<T2>();
//        var s3 = world.Store<T3>();
//        var s4 = world.Store<T4>();
//        EnsureCache(world, desc);
//        var none = desc.CachedNone!;
//        var any = desc.CachedAny!;

//        if (s1.Count == s2.Count && s2.Count == s3.Count && s3.Count == s4.Count
//            && none.Length == 0 && any.Length == 0)
//        {
//            for (int i = s1.Count - 1; i >= 0; i--)
//                callback(s1.EntityAt(i),
//                    ref s1.GetAtDenseIndex(i),
//                    ref s2.GetAtDenseIndex(i),
//                    ref s3.GetAtDenseIndex(i),
//                    ref s4.GetAtDenseIndex(i));
//            return;
//        }

//        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count);
//        switch (pivotIdx)
//        {
//            case 0:
//                for (int i = s1.Count - 1; i >= 0; i--)
//                {
//                    EntityId e = s1.EntityAt(i);
//                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
//                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
//                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
//                    if (AnyHas(none, e)) continue;
//                    if (!PassesAny(any, e)) continue;
//                    callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
//                }
//                break;
//            case 1:
//                for (int i = s2.Count - 1; i >= 0; i--)
//                {
//                    EntityId e = s2.EntityAt(i);
//                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
//                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
//                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
//                    if (AnyHas(none, e)) continue;
//                    if (!PassesAny(any, e)) continue;
//                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
//                }
//                break;
//            case 2:
//                for (int i = s3.Count - 1; i >= 0; i--)
//                {
//                    EntityId e = s3.EntityAt(i);
//                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
//                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
//                    int p4 = s4.GetDenseIndexFast(e); if (p4 < 0) continue;
//                    if (AnyHas(none, e)) continue;
//                    if (!PassesAny(any, e)) continue;
//                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i), ref s4.GetAtDenseIndex(p4));
//                }
//                break;
//            default:
//                for (int i = s4.Count - 1; i >= 0; i--)
//                {
//                    EntityId e = s4.EntityAt(i);
//                    int p1 = s1.GetDenseIndexFast(e); if (p1 < 0) continue;
//                    int p2 = s2.GetDenseIndexFast(e); if (p2 < 0) continue;
//                    int p3 = s3.GetDenseIndexFast(e); if (p3 < 0) continue;
//                    if (AnyHas(none, e)) continue;
//                    if (!PassesAny(any, e)) continue;
//                    callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(i));
//                }
//                break;
//        }
//    }

//    // =========================================================================
//    // Cache management
//    // =========================================================================

//    private static void EnsureCache(World world, QueryDescription desc)
//    {
//        if (ReferenceEquals(desc.CachedWorld, world)) return;
//        desc.CachedNone = BuildPredicates(world, desc.None);
//        desc.CachedAny = BuildPredicates(world, desc.Any);
//        desc.CachedWorld = world;
//    }

//    private static HasPredicate[] BuildPredicates(World world, List<Type> types)
//    {
//        if (types.Count == 0) return Array.Empty<HasPredicate>();
//        var result = new HasPredicate[types.Count];
//        for (int i = 0; i < types.Count; i++)
//            result[i] = new HasPredicate(world.StoreByType(types[i]));
//        return result;
//    }

//    // =========================================================================
//    // Hot-loop filter helpers
//    // =========================================================================

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static bool AnyHas(HasPredicate[] predicates, EntityId entity)
//    {
//        for (int i = 0; i < predicates.Length; i++)
//            if (predicates[i].Has(entity)) return true;
//        return false;
//    }

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static bool PassesAny(HasPredicate[] predicates, EntityId entity)
//    {
//        if (predicates.Length == 0) return true;
//        for (int i = 0; i < predicates.Length; i++)
//            if (predicates[i].Has(entity)) return true;
//        return false;
//    }

//    // =========================================================================
//    // Pivot selection
//    // =========================================================================

//    private static int MinIndex(int a, int b, int c)
//    {
//        if (a <= b && a <= c) return 0;
//        if (b <= c) return 1;
//        return 2;
//    }

//    private static int MinIndex(int a, int b, int c, int d)
//    {
//        int idx3 = MinIndex(a, b, c);
//        int val3 = idx3 switch { 0 => a, 1 => b, _ => c };
//        return d < val3 ? 3 : idx3;
//    }
//}

/*
/// <summary>
/// Extension methods that add delegate-based Query overloads to <see cref="World"/>.
///
/// Performance design (see OPTIMISATIONS section below for rationale):
///   1. Filter predicates are cached on QueryDescription as Func<EntityId,bool>
///      arrays — resolved once, zero allocation on subsequent calls.
///   2. IsAlive() is NOT called in the hot loop — the dense-index generation
///      check in GetDenseIndex already rejects stale entities, and DestroyEntity
///      removes the entity from all stores so it can never appear in a valid pivot.
///   3. The pivot store is iterated by raw index so its dense position is known
///      for free — no redundant sparse lookup for the pivot component.
///   4. AnyHas / PassesAny call through concrete Func delegates, not IComponentStore
///      interface dispatch — the JIT can devirtualise and inline Has().
/// </summary>
public static class WorldQueryExtensions
{
    // =========================================================================
    // Public Query overloads
    // =========================================================================

    /// <summary>Single-component query.</summary>
    public static void Query<T1>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1> callback)
    {
        var s1 = world.Store<T1>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        // Iterate by raw index — position i IS the dense index for the pivot.
        //int count = s1.Count;
        //for (int i = 0; i < count; i++)
        for (int i = s1.Count - 1; i >= 0; i--)
        {
            EntityId e = s1.EntityAt(i);

            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;

            callback(e, ref s1.GetAtDenseIndex(i));
        }
    }

    /// <summary>Two-component query.</summary>
    public static void Query<T1, T2>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        bool pivotIs1 = s1.Count <= s2.Count;

        if (pivotIs1)
        {
            int count = s1.Count;
            for (int i = 0; i < count; i++)
            {
                EntityId e = s1.EntityAt(i);
                int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;

                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;

                callback(e,
                    ref s1.GetAtDenseIndex(i),   // i IS p1 — no sparse lookup
                    ref s2.GetAtDenseIndex(p2));
            }
        }
        else
        {
            int count = s2.Count;
            for (int i = 0; i < count; i++)
            {
                EntityId e = s2.EntityAt(i);
                int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;

                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;

                callback(e,
                    ref s1.GetAtDenseIndex(p1),
                    ref s2.GetAtDenseIndex(i));  // i IS p2
            }
        }
    }

    /// <summary>Three-component query.</summary>
    public static void Query<T1, T2, T3>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count);

        switch (pivotIdx)
        {
            case 0:
                {
                    int count = s1.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s1.EntityAt(i);
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3));
                    }
                    break;
                }
            case 1:
                {
                    int count = s2.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s2.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3));
                    }
                    break;
                }
            default:
                {
                    int count = s3.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s3.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i));
                    }
                    break;
                }
        }
    }

    /// <summary>Four-component query.</summary>
    public static void Query<T1, T2, T3, T4>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3, T4> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var s4 = world.Store<T4>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count);

        switch (pivotIdx)
        {
            case 0:
                {
                    int count = s1.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s1.EntityAt(i);
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        int p4 = s4.GetDenseIndex(e); if (p4 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
                    }
                    break;
                }
            case 1:
                {
                    int count = s2.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s2.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        int p4 = s4.GetDenseIndex(e); if (p4 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
                    }
                    break;
                }
            case 2:
                {
                    int count = s3.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s3.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p4 = s4.GetDenseIndex(e); if (p4 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i), ref s4.GetAtDenseIndex(p4));
                    }
                    break;
                }
            default:
                {
                    int count = s4.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s4.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(i));
                    }
                    break;
                }
        }
    }

    // =========================================================================
    // Cache management
    // =========================================================================

    /// <summary>
    /// Ensures the QueryDescription's filter predicates are resolved for the
    /// given world.  A no-op on every call after the first (for the same world).
    ///
    /// Stores as Func<EntityId,bool> delegates capturing the concrete
    /// ComponentStore<T> — the JIT can devirtualise and inline Has() through
    /// a delegate far more effectively than through an IComponentStore interface.
    /// </summary>
    private static void EnsureCache(World world, QueryDescription desc)
    {
        if (desc.CachedWorld == world) return;   // already resolved for this world

        desc.CachedNonePredicates = BuildPredicates(world, desc.None);
        desc.CachedAnyPredicates = BuildPredicates(world, desc.Any);
        desc.CachedWorld = world;
    }

    /// <summary>
    /// Builds a predicate array for a list of component types.
    /// Each predicate captures the concrete ComponentStore<T> by reference,
    /// so the JIT can see the real type and inline Has() in the hot loop.
    /// </summary>
    private static Func<EntityId, bool>[] BuildPredicates(World world, List<Type> types)
    {
        if (types.Count == 0) return Array.Empty<Func<EntityId, bool>>();

        var result = new Func<EntityId, bool>[types.Count];
        for (int i = 0; i < types.Count; i++)
        {
            // StoreByType returns the concrete ComponentStore<T> boxed as IComponentStore.
            // We wrap Has() in a lambda that captures the concrete store reference.
            // This avoids repeated interface dispatch in the hot loop.
            var store = world.StoreByType(types[i]);
            result[i] = store.Has;    // IComponentStore.Has — delegate, not vtable call per-entity
        }
        return result;
    }

    // =========================================================================
    // Hot-loop filter helpers
    // =========================================================================

    /// <summary>
    /// Returns true if ANY predicate in the array matches the entity.
    /// Empty array (no WithNone constraints) returns false immediately.
    /// </summary>
    private static bool AnyHas(Func<EntityId, bool>[] predicates, EntityId entity)
    {
        // Manual loop over known-small array — avoids enumerator overhead.
        for (int i = 0; i < predicates.Length; i++)
            if (predicates[i](entity)) return true;
        return false;
    }

    /// <summary>
    /// Returns true if the entity passes the WithAny filter:
    ///   • Empty array (no WithAny constraints) → always passes.
    ///   • Otherwise → at least one predicate must match.
    /// </summary>
    private static bool PassesAny(Func<EntityId, bool>[] predicates, EntityId entity)
    {
        if (predicates.Length == 0) return true;
        for (int i = 0; i < predicates.Length; i++)
            if (predicates[i](entity)) return true;
        return false;
    }

    // =========================================================================
    // Pivot helpers
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
*/
/*
/// <summary>
/// Extension methods that add delegate-based Query overloads to <see cref="World"/>.
///
/// Must live in the same assembly as World so it can access the internal
/// <see cref="World.IComponentStore"/> interface and
/// <see cref="ComponentStore{T}.GetDenseIndex"/> /
/// <see cref="ComponentStore{T}.GetAtDenseIndex"/>.
/// </summary>
public static class WorldQueryExtensions
{
    // =========================================================================
    // Public Query overloads
    // =========================================================================

    /// <summary>Single-component query.</summary>
    public static void Query<T1>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1> callback)
    {
        var s1 = world.Store<T1>();
        var none = ResolveStores(world, desc.None);
        var any = ResolveStores(world, desc.Any);

        // T1 is always in WithAll, so s1 is the pivot — iterate its packed span.
        foreach (EntityId e in s1.Entities)
        {
            if (!world.IsAlive(e)) continue;
            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;

            int p1 = s1.GetDenseIndex(e);   // always >= 0 (came from s1.Entities)
            callback(e, ref s1.GetAtDenseIndex(p1));
        }
    }

    /// <summary>Two-component query.</summary>
    public static void Query<T1, T2>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var none = ResolveStores(world, desc.None);
        var any = ResolveStores(world, desc.Any);

        bool pivotIs1 = s1.Count <= s2.Count;
        ReadOnlySpan<EntityId> candidates = pivotIs1 ? s1.Entities : s2.Entities;

        foreach (EntityId e in candidates)
        {
            if (!world.IsAlive(e)) continue;

            int p1, p2;
            if (pivotIs1)
            {
                p1 = s1.GetDenseIndex(e);
                p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
            }
            else
            {
                p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                p2 = s2.GetDenseIndex(e);
            }

            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;

            callback(e,
                ref s1.GetAtDenseIndex(p1),
                ref s2.GetAtDenseIndex(p2));
        }
    }

    /// <summary>Three-component query.</summary>
    public static void Query<T1, T2, T3>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var none = ResolveStores(world, desc.None);
        var any = ResolveStores(world, desc.Any);

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count);
        ReadOnlySpan<EntityId> candidates = pivotIdx switch
        {
            0 => s1.Entities,
            1 => s2.Entities,
            _ => s3.Entities,
        };

        foreach (EntityId e in candidates)
        {
            if (!world.IsAlive(e)) continue;

            int p1 = s1.GetDenseIndex(e); if (pivotIdx != 0 && p1 < 0) continue;
            int p2 = s2.GetDenseIndex(e); if (pivotIdx != 1 && p2 < 0) continue;
            int p3 = s3.GetDenseIndex(e); if (pivotIdx != 2 && p3 < 0) continue;

            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;

            callback(e,
                ref s1.GetAtDenseIndex(p1),
                ref s2.GetAtDenseIndex(p2),
                ref s3.GetAtDenseIndex(p3));
        }
    }

    /// <summary>Four-component query.</summary>
    public static void Query<T1, T2, T3, T4>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3, T4> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var s4 = world.Store<T4>();
        var none = ResolveStores(world, desc.None);
        var any = ResolveStores(world, desc.Any);

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count);
        ReadOnlySpan<EntityId> candidates = pivotIdx switch
        {
            0 => s1.Entities,
            1 => s2.Entities,
            2 => s3.Entities,
            _ => s4.Entities,
        };

        foreach (EntityId e in candidates)
        {
            if (!world.IsAlive(e)) continue;

            int p1 = s1.GetDenseIndex(e); if (pivotIdx != 0 && p1 < 0) continue;
            int p2 = s2.GetDenseIndex(e); if (pivotIdx != 1 && p2 < 0) continue;
            int p3 = s3.GetDenseIndex(e); if (pivotIdx != 2 && p3 < 0) continue;
            int p4 = s4.GetDenseIndex(e); if (pivotIdx != 3 && p4 < 0) continue;

            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;

            callback(e,
                ref s1.GetAtDenseIndex(p1),
                ref s2.GetAtDenseIndex(p2),
                ref s3.GetAtDenseIndex(p3),
                ref s4.GetAtDenseIndex(p4));
        }
    }

    ///// <summary>Five-component query.</summary>
    //public static void Query<T1, T2, T3, T4, T5>(
    //    this World world,
    //    in QueryDescription desc,
    //    QueryCallback<T1, T2, T3, T4, T5> callback)
    //{
    //    var s1 = world.Store<T1>();
    //    var s2 = world.Store<T2>();
    //    var s3 = world.Store<T3>();
    //    var s4 = world.Store<T4>();
    //    var s5 = world.Store<T5>();
    //    var none = ResolveStores(world, desc.None);
    //    var any = ResolveStores(world, desc.Any);

    //    int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count, s5.Count);
    //    ReadOnlySpan<EntityId> candidates = pivotIdx switch
    //    {
    //        0 => s1.Entities,
    //        1 => s2.Entities,
    //        2 => s3.Entities,
    //        3 => s4.Entities,
    //        _ => s5.Entities,
    //    };

    //    foreach (EntityId e in candidates)
    //    {
    //        if (!world.IsAlive(e)) continue;

    //        int p1 = s1.GetDenseIndex(e); if (pivotIdx != 0 && p1 < 0) continue;
    //        int p2 = s2.GetDenseIndex(e); if (pivotIdx != 1 && p2 < 0) continue;
    //        int p3 = s3.GetDenseIndex(e); if (pivotIdx != 2 && p3 < 0) continue;
    //        int p4 = s4.GetDenseIndex(e); if (pivotIdx != 3 && p4 < 0) continue;
    //        int p5 = s5.GetDenseIndex(e); if (pivotIdx != 4 && p5 < 0) continue;

    //        if (AnyHas(none, e)) continue;
    //        if (!PassesAny(any, e)) continue;

    //        callback(e,
    //            ref s1.GetAtDenseIndex(p1),
    //            ref s2.GetAtDenseIndex(p2),
    //            ref s3.GetAtDenseIndex(p3),
    //            ref s4.GetAtDenseIndex(p4),
    //            ref s5.GetAtDenseIndex(p5));
    //    }
    //}

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>
    /// Resolves a list of types to their IComponentStore instances.
    /// Returns <see cref="Array.Empty{T}"/> when the list is empty so callers
    /// can short-circuit with zero allocation.
    /// </summary>
    private static World.IComponentStore[] ResolveStores(World world, List<Type> types)
    {
        if (types.Count == 0) return Array.Empty<World.IComponentStore>();

        var result = new World.IComponentStore[types.Count];
        for (int i = 0; i < types.Count; i++)
            result[i] = world.StoreByType(types[i]);
        return result;
    }

    /// <summary>
    /// Returns true if ANY store in <paramref name="stores"/> has the entity.
    /// Used for WithNone — caller negates the result.
    /// Length 0 returns false immediately.
    /// </summary>
    private static bool AnyHas(World.IComponentStore[] stores, EntityId entity)
    {
        for (int i = 0; i < stores.Length; i++)
            if (stores[i].Has(entity)) return true;
        return false;
    }

    /// <summary>
    /// Returns true if the entity passes the WithAny filter:
    ///   • No WithAny constraints  → always passes (empty array = no filter).
    ///   • WithAny constraints present → entity must have at least one.
    /// </summary>
    private static bool PassesAny(World.IComponentStore[] anyStores, EntityId entity)
    {
        // No WithAny constraint at all — entity passes unconditionally.
        if (anyStores.Length == 0) return true;

        // At least one of the WithAny types must be present.
        for (int i = 0; i < anyStores.Length; i++)
            if (anyStores[i].Has(entity)) return true;

        return false;
    }

    /// <summary>Returns the 0-based index of the smallest value among the arguments.</summary>
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

    private static int MinIndex(int a, int b, int c, int d, int e)
    {
        int idx4 = MinIndex(a, b, c, d);
        int val4 = idx4 switch { 0 => a, 1 => b, 2 => c, _ => d };
        return e < val4 ? 4 : idx4;
    }
}
*/
/*
/*
/// <summary>
/// Extension methods that add delegate-based Query overloads to <see cref="World"/>.
///
/// Performance design (see OPTIMISATIONS section below for rationale):
///   1. Filter predicates are cached on QueryDescription as Func<EntityId,bool>
///      arrays — resolved once, zero allocation on subsequent calls.
///   2. IsAlive() is NOT called in the hot loop — the dense-index generation
///      check in GetDenseIndex already rejects stale entities, and DestroyEntity
///      removes the entity from all stores so it can never appear in a valid pivot.
///   3. The pivot store is iterated by raw index so its dense position is known
///      for free — no redundant sparse lookup for the pivot component.
///   4. AnyHas / PassesAny call through concrete Func delegates, not IComponentStore
///      interface dispatch — the JIT can devirtualise and inline Has().
/// </summary>
public static class WorldQueryExtensions
{
    // =========================================================================
    // Public Query overloads
    // =========================================================================

    /// <summary>Single-component query.</summary>
    public static void Query<T1>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1> callback)
    {
        var s1 = world.Store<T1>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        // Iterate by raw index — position i IS the dense index for the pivot.
        int count = s1.Count;
        for (int i = 0; i < count; i++)
        {
            EntityId e = s1.EntityAt(i);

            if (AnyHas(none, e)) continue;
            if (!PassesAny(any, e)) continue;

            callback(e, ref s1.GetAtDenseIndex(i));
        }
    }

    /// <summary>Two-component query.</summary>
    public static void Query<T1, T2>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        bool pivotIs1 = s1.Count <= s2.Count;

        if (pivotIs1)
        {
            int count = s1.Count;
            for (int i = 0; i < count; i++)
            {
                EntityId e = s1.EntityAt(i);
                int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;

                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;

                callback(e,
                    ref s1.GetAtDenseIndex(i),   // i IS p1 — no sparse lookup
                    ref s2.GetAtDenseIndex(p2));
            }
        }
        else
        {
            int count = s2.Count;
            for (int i = 0; i < count; i++)
            {
                EntityId e = s2.EntityAt(i);
                int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;

                if (AnyHas(none, e)) continue;
                if (!PassesAny(any, e)) continue;

                callback(e,
                    ref s1.GetAtDenseIndex(p1),
                    ref s2.GetAtDenseIndex(i));  // i IS p2
            }
        }
    }

    /// <summary>Three-component query.</summary>
    public static void Query<T1, T2, T3>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count);

        switch (pivotIdx)
        {
            case 0:
                {
                    int count = s1.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s1.EntityAt(i);
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3));
                    }
                    break;
                }
            case 1:
                {
                    int count = s2.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s2.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3));
                    }
                    break;
                }
            default:
                {
                    int count = s3.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s3.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i));
                    }
                    break;
                }
        }
    }

    /// <summary>Four-component query.</summary>
    public static void Query<T1, T2, T3, T4>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3, T4> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var s4 = world.Store<T4>();

        EnsureCache(world, desc);
        var none = desc.CachedNonePredicates!;
        var any = desc.CachedAnyPredicates!;

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count);

        switch (pivotIdx)
        {
            case 0:
                {
                    int count = s1.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s1.EntityAt(i);
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        int p4 = s4.GetDenseIndex(e); if (p4 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(i), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
                    }
                    break;
                }
            case 1:
                {
                    int count = s2.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s2.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        int p4 = s4.GetDenseIndex(e); if (p4 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(i), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(p4));
                    }
                    break;
                }
            case 2:
                {
                    int count = s3.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s3.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p4 = s4.GetDenseIndex(e); if (p4 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(i), ref s4.GetAtDenseIndex(p4));
                    }
                    break;
                }
            default:
                {
                    int count = s4.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EntityId e = s4.EntityAt(i);
                        int p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                        int p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
                        int p3 = s3.GetDenseIndex(e); if (p3 < 0) continue;
                        if (AnyHas(none, e)) continue;
                        if (!PassesAny(any, e)) continue;
                        callback(e, ref s1.GetAtDenseIndex(p1), ref s2.GetAtDenseIndex(p2), ref s3.GetAtDenseIndex(p3), ref s4.GetAtDenseIndex(i));
                    }
                    break;
                }
        }
    }

    // =========================================================================
    // Cache management
    // =========================================================================

    /// <summary>
    /// Ensures the QueryDescription's filter predicates are resolved for the
    /// given world.  A no-op on every call after the first (for the same world).
    ///
    /// Stores as Func<EntityId,bool> delegates capturing the concrete
    /// ComponentStore<T> — the JIT can devirtualise and inline Has() through
    /// a delegate far more effectively than through an IComponentStore interface.
    /// </summary>
    private static void EnsureCache(World world, QueryDescription desc)
    {
        if (desc.CachedWorld == world) return;   // already resolved for this world

        desc.CachedNonePredicates = BuildPredicates(world, desc.None);
        desc.CachedAnyPredicates = BuildPredicates(world, desc.Any);
        desc.CachedWorld = world;
    }

    /// <summary>
    /// Builds a predicate array for a list of component types.
    /// Each predicate captures the concrete ComponentStore<T> by reference,
    /// so the JIT can see the real type and inline Has() in the hot loop.
    /// </summary>
    private static Func<EntityId, bool>[] BuildPredicates(World world, List<Type> types)
    {
        if (types.Count == 0) return Array.Empty<Func<EntityId, bool>>();

        var result = new Func<EntityId, bool>[types.Count];
        for (int i = 0; i < types.Count; i++)
        {
            // StoreByType returns the concrete ComponentStore<T> boxed as IComponentStore.
            // We wrap Has() in a lambda that captures the concrete store reference.
            // This avoids repeated interface dispatch in the hot loop.
            var store = world.StoreByType(types[i]);
            result[i] = store.Has;    // IComponentStore.Has — delegate, not vtable call per-entity
        }
        return result;
    }

    // =========================================================================
    // Hot-loop filter helpers
    // =========================================================================

    /// <summary>
    /// Returns true if ANY predicate in the array matches the entity.
    /// Empty array (no WithNone constraints) returns false immediately.
    /// </summary>
    private static bool AnyHas(Func<EntityId, bool>[] predicates, EntityId entity)
    {
        // Manual loop over known-small array — avoids enumerator overhead.
        for (int i = 0; i < predicates.Length; i++)
            if (predicates[i](entity)) return true;
        return false;
    }

    /// <summary>
    /// Returns true if the entity passes the WithAny filter:
    ///   • Empty array (no WithAny constraints) → always passes.
    ///   • Otherwise → at least one predicate must match.
    /// </summary>
    private static bool PassesAny(Func<EntityId, bool>[] predicates, EntityId entity)
    {
        if (predicates.Length == 0) return true;
        for (int i = 0; i < predicates.Length; i++)
            if (predicates[i](entity)) return true;
        return false;
    }

    // =========================================================================
    // Pivot helpers
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
/*
/// <summary>
/// Extension methods that add delegate-based Query overloads to <see cref="World"/>.
///
/// Must live in the same assembly as World so it can access the internal
/// <see cref="World.IComponentStore"/> interface and
/// <see cref="ComponentStore{T}.GetDenseIndex"/> /
/// <see cref="ComponentStore{T}.GetAtDenseIndex"/>.
/// </summary>
public static class WorldQueryExtensions
{
    // =========================================================================
    // Public Query overloads
    // =========================================================================

    /// <summary>Single-component query.</summary>
    public static void Query<T1>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1> callback)
    {
        var s1 = world.Store<T1>();
        var none = ResolveNoneStores(world, desc.None);

        // Only one required store — it is always the pivot, no extra lookup needed.
        foreach (EntityId e in s1.Entities)
        {
            if (!world.IsAlive(e)) continue;
            if (AnyHas(none, e)) continue;

            int p1 = s1.GetDenseIndex(e);   // always >= 0 (came from s1.Entities)
            callback(e, ref s1.GetAtDenseIndex(p1));
        }
    }

    /// <summary>Two-component query.</summary>
    public static void Query<T1, T2>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var none = ResolveNoneStores(world, desc.None);

        // Pivot = smallest store.  We record WHICH store won so we can skip
        // its GetDenseIndex check inside the loop (the entity is guaranteed present).
        bool pivotIs1 = s1.Count <= s2.Count;
        ReadOnlySpan<EntityId> candidates = pivotIs1 ? s1.Entities : s2.Entities;

        foreach (EntityId e in candidates)
        {
            if (!world.IsAlive(e)) continue;

            int p1, p2;
            if (pivotIs1)
            {
                p1 = s1.GetDenseIndex(e);           // always >= 0
                p2 = s2.GetDenseIndex(e); if (p2 < 0) continue;
            }
            else
            {
                p1 = s1.GetDenseIndex(e); if (p1 < 0) continue;
                p2 = s2.GetDenseIndex(e);           // always >= 0
            }

            if (AnyHas(none, e)) continue;

            callback(e,
                ref s1.GetAtDenseIndex(p1),
                ref s2.GetAtDenseIndex(p2));
        }
    }

    /// <summary>Three-component query.</summary>
    public static void Query<T1, T2, T3>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var none = ResolveNoneStores(world, desc.None);

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count);
        ReadOnlySpan<EntityId> candidates = pivotIdx switch
        {
            0 => s1.Entities,
            1 => s2.Entities,
            _ => s3.Entities,
        };

        foreach (EntityId e in candidates)
        {
            if (!world.IsAlive(e)) continue;

            int p1 = s1.GetDenseIndex(e); if (pivotIdx != 0 && p1 < 0) continue;
            int p2 = s2.GetDenseIndex(e); if (pivotIdx != 1 && p2 < 0) continue;
            int p3 = s3.GetDenseIndex(e); if (pivotIdx != 2 && p3 < 0) continue;

            if (AnyHas(none, e)) continue;

            callback(e,
                ref s1.GetAtDenseIndex(p1),
                ref s2.GetAtDenseIndex(p2),
                ref s3.GetAtDenseIndex(p3));
        }
    }

    /// <summary>Four-component query.</summary>
    public static void Query<T1, T2, T3, T4>(
        this World world,
        in QueryDescription desc,
        QueryCallback<T1, T2, T3, T4> callback)
    {
        var s1 = world.Store<T1>();
        var s2 = world.Store<T2>();
        var s3 = world.Store<T3>();
        var s4 = world.Store<T4>();
        var none = ResolveNoneStores(world, desc.None);

        int pivotIdx = MinIndex(s1.Count, s2.Count, s3.Count, s4.Count);
        ReadOnlySpan<EntityId> candidates = pivotIdx switch
        {
            0 => s1.Entities,
            1 => s2.Entities,
            2 => s3.Entities,
            _ => s4.Entities,
        };

        foreach (EntityId e in candidates)
        {
            if (!world.IsAlive(e)) continue;

            int p1 = s1.GetDenseIndex(e); if (pivotIdx != 0 && p1 < 0) continue;
            int p2 = s2.GetDenseIndex(e); if (pivotIdx != 1 && p2 < 0) continue;
            int p3 = s3.GetDenseIndex(e); if (pivotIdx != 2 && p3 < 0) continue;
            int p4 = s4.GetDenseIndex(e); if (pivotIdx != 3 && p4 < 0) continue;

            if (AnyHas(none, e)) continue;

            callback(e,
                ref s1.GetAtDenseIndex(p1),
                ref s2.GetAtDenseIndex(p2),
                ref s3.GetAtDenseIndex(p3),
                ref s4.GetAtDenseIndex(p4));
        }
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>
    /// Resolves WithNone types to IComponentStore instances without knowing T.
    /// Returns Array.Empty when the list is empty — AnyHas short-circuits immediately.
    /// </summary>
    private static World.IComponentStore[] ResolveNoneStores(
        World world, List<Type> noneTypes)
    {
        if (noneTypes.Count == 0) return Array.Empty<World.IComponentStore>();

        var result = new World.IComponentStore[noneTypes.Count];
        for (int i = 0; i < noneTypes.Count; i++)
            result[i] = world.StoreByType(noneTypes[i]);
        return result;
    }

    /// <summary>
    /// Returns true if any store in <paramref name="noneStores"/> has the entity.
    /// Length 0 returns false without any iteration.
    /// </summary>
    private static bool AnyHas(World.IComponentStore[] noneStores, EntityId entity)
    {
        for (int i = 0; i < noneStores.Length; i++)
            if (noneStores[i].Has(entity)) return true;
        return false;
    }

    /// <summary>Returns the 0-based index of the smallest value among the arguments.</summary>
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
}*/