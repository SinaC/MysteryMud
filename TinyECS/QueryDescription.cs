using TinyECS.Extensions;

namespace TinyECS;

/// <summary>
/// Declarative, reusable query spec.  Build once, pass to World.Query() every tick.
///
///   var desc = new QueryDescription()
///       .WithAll&lt;Position, Velocity&gt;()
///       .WithAny&lt;CombatState, Stunned&gt;()
///       .WithNone&lt;Dead&gt;();
///
/// Filter predicates are resolved lazily on the first Query() call and then
/// cached for the lifetime of the QueryDescription.  Subsequent calls pay
/// zero allocation and zero virtual dispatch in the hot loop.
/// </summary>
public sealed class QueryDescription
{
    internal readonly List<Type> All = new();
    internal readonly List<Type> Any = new();
    internal readonly List<Type> None = new();

    // -----------------------------------------------------------------------
    // Filter cache
    //
    // Resolved once on first Query() call, then frozen.
    // Stored as HasPredicate[] (value-type wrappers around IComponentStore) so
    // the hot loop avoids interface vtable dispatch on every Has() call.
    //
    // CachedWorld lets us detect if the same QueryDescription is reused
    // with a different World instance and invalidate accordingly.
    // -----------------------------------------------------------------------

    internal WorldQueryExtensions.HasPredicate[]? CachedNone;
    internal WorldQueryExtensions.HasPredicate[]? CachedAny;
    internal World? CachedWorld;

    // ------------------------------------------------------------------
    // WithAll — all listed components must be present
    // ------------------------------------------------------------------

    public QueryDescription WithAll<T1>()
    {
        All.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithAll<T1, T2>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithAll<T1, T2, T3>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2)); All.Add(typeof(T3));
        return this;
    }

    public QueryDescription WithAll<T1, T2, T3, T4>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2));
        All.Add(typeof(T3)); All.Add(typeof(T4));
        return this;
    }

    // ------------------------------------------------------------------
    // WithAny — at least one of the listed components must be present
    // ------------------------------------------------------------------

    public QueryDescription WithAny<T1>()
    {
        Any.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithAny<T1, T2>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithAny<T1, T2, T3>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2)); Any.Add(typeof(T3));
        return this;
    }

    public QueryDescription WithAny<T1, T2, T3, T4>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2));
        Any.Add(typeof(T3)); Any.Add(typeof(T4));
        return this;
    }

    // ------------------------------------------------------------------
    // WithNone — none of the listed components may be present
    // ------------------------------------------------------------------

    public QueryDescription WithNone<T1>()
    {
        None.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithNone<T1, T2>()
    {
        None.Add(typeof(T1)); None.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithNone<T1, T2, T3>()
    {
        None.Add(typeof(T1)); None.Add(typeof(T2)); None.Add(typeof(T3));
        return this;
    }
}

// -----------------------------------------------------------------------
// Ref-delegate types (one per arity).
// Standard Action<> / Func<> do not support ref parameters.
// -----------------------------------------------------------------------

public delegate void QueryCallback<T1>(
    EntityId entity,
    ref T1 c1);

public delegate void QueryCallback<T1, T2>(
    EntityId entity,
    ref T1 c1, ref T2 c2);

public delegate void QueryCallback<T1, T2, T3>(
    EntityId entity,
    ref T1 c1, ref T2 c2, ref T3 c3);

public delegate void QueryCallback<T1, T2, T3, T4>(
    EntityId entity,
    ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);

/*
/// <summary>
/// Declarative, reusable query spec.  Build once, pass to World.Query() every tick.
///
///   var desc = new QueryDescription()
///       .WithAll<Position, Velocity>()
///       .WithAny<CombatState, Stunned>()   // at least one must be present
///       .WithNone<Dead>();
/// </summary>
public sealed class QueryDescription
{
    internal readonly List<Type> All = new();
    internal readonly List<Type> Any = new();
    internal readonly List<Type> None = new();

    // ------------------------------------------------------------------
    // WithAll — all listed components must be present
    // ------------------------------------------------------------------

    public QueryDescription WithAll<T1>()
    {
        All.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithAll<T1, T2>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithAll<T1, T2, T3>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2)); All.Add(typeof(T3));
        return this;
    }

    public QueryDescription WithAll<T1, T2, T3, T4>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2));
        All.Add(typeof(T3)); All.Add(typeof(T4));
        return this;
    }

    public QueryDescription WithAll<T1, T2, T3, T4, T5>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2));
        All.Add(typeof(T3)); All.Add(typeof(T4));
        All.Add(typeof(T5));
        return this;
    }

    // ------------------------------------------------------------------
    // WithAny — at least one of the listed components must be present.
    // The entity passes if it has ANY of the types; it fails only if it
    // has NONE of them.
    // ------------------------------------------------------------------

    public QueryDescription WithAny<T1>()
    {
        Any.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithAny<T1, T2>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithAny<T1, T2, T3>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2)); Any.Add(typeof(T3));
        return this;
    }

    public QueryDescription WithAny<T1, T2, T3, T4>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2));
        Any.Add(typeof(T3)); Any.Add(typeof(T4));
        return this;
    }

    // ------------------------------------------------------------------
    // WithNone — none of the listed components may be present
    // ------------------------------------------------------------------

    public QueryDescription WithNone<T1>()
    {
        None.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithNone<T1, T2>()
    {
        None.Add(typeof(T1)); None.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithNone<T1, T2, T3>()
    {
        None.Add(typeof(T1)); None.Add(typeof(T2)); None.Add(typeof(T3));
        return this;
    }
}

// -----------------------------------------------------------------------
// Ref-delegate types (one per arity).
// Standard Action<> / Func<> do not support ref parameters, so we need
// custom delegate types.  These are the only supported arities; add more
// if needed following the same pattern.
// -----------------------------------------------------------------------

public delegate void QueryCallback<T1>(
    EntityId entity,
    ref T1 c1);

public delegate void QueryCallback<T1, T2>(
    EntityId entity,
    ref T1 c1, ref T2 c2);

public delegate void QueryCallback<T1, T2, T3>(
    EntityId entity,
    ref T1 c1, ref T2 c2, ref T3 c3);

public delegate void QueryCallback<T1, T2, T3, T4>(
    EntityId entity,
    ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);

public delegate void QueryCallback<T1, T2, T3, T4, T5>(
    EntityId entity,
    ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5);
*/
/*
/// <summary>
/// Declarative, reusable query spec.  Build once, pass to World.Query() every tick.
///
///   var desc = new QueryDescription()
///       .WithAll&lt;Position, Velocity&gt;()
///       .WithAny&lt;CombatState, Stunned&gt;()
///       .WithNone&lt;Dead&gt;();
///
/// Filter predicates are resolved lazily on the first Query() call and then
/// cached for the lifetime of the QueryDescription.  Subsequent calls pay
/// zero allocation and zero virtual dispatch in the hot loop.
/// </summary>
public sealed class QueryDescription
{
    internal readonly List<Type> All = new();
    internal readonly List<Type> Any = new();
    internal readonly List<Type> None = new();

    // -----------------------------------------------------------------------
    // Filter cache
    //
    // Resolved once on first Query() call, then frozen.
    // Stored as Func<EntityId,bool> arrays so the JIT sees a concrete
    // delegate target and can devirtualise / inline the Has() call — unlike
    // calling IComponentStore.Has() through an interface.
    //
    // CachedWorld lets us detect if the same QueryDescription is reused
    // with a different World instance and invalidate accordingly.
    // -----------------------------------------------------------------------

    internal Func<EntityId, bool>[]? CachedNonePredicates;
    internal Func<EntityId, bool>[]? CachedAnyPredicates;
    internal World? CachedWorld;

    // ------------------------------------------------------------------
    // WithAll — all listed components must be present
    // ------------------------------------------------------------------

    public QueryDescription WithAll<T1>()
    {
        All.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithAll<T1, T2>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithAll<T1, T2, T3>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2)); All.Add(typeof(T3));
        return this;
    }

    public QueryDescription WithAll<T1, T2, T3, T4>()
    {
        All.Add(typeof(T1)); All.Add(typeof(T2));
        All.Add(typeof(T3)); All.Add(typeof(T4));
        return this;
    }

    // ------------------------------------------------------------------
    // WithAny — at least one of the listed components must be present
    // ------------------------------------------------------------------

    public QueryDescription WithAny<T1>()
    {
        Any.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithAny<T1, T2>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithAny<T1, T2, T3>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2)); Any.Add(typeof(T3));
        return this;
    }

    public QueryDescription WithAny<T1, T2, T3, T4>()
    {
        Any.Add(typeof(T1)); Any.Add(typeof(T2));
        Any.Add(typeof(T3)); Any.Add(typeof(T4));
        return this;
    }

    // ------------------------------------------------------------------
    // WithNone — none of the listed components may be present
    // ------------------------------------------------------------------

    public QueryDescription WithNone<T1>()
    {
        None.Add(typeof(T1));
        return this;
    }

    public QueryDescription WithNone<T1, T2>()
    {
        None.Add(typeof(T1)); None.Add(typeof(T2));
        return this;
    }

    public QueryDescription WithNone<T1, T2, T3>()
    {
        None.Add(typeof(T1)); None.Add(typeof(T2)); None.Add(typeof(T3));
        return this;
    }
}

// -----------------------------------------------------------------------
// Ref-delegate types (one per arity).
// Standard Action<> / Func<> do not support ref parameters.
// -----------------------------------------------------------------------

public delegate void QueryCallback<T1>(
    EntityId entity,
    ref T1 c1);

public delegate void QueryCallback<T1, T2>(
    EntityId entity,
    ref T1 c1, ref T2 c2);

public delegate void QueryCallback<T1, T2, T3>(
    EntityId entity,
    ref T1 c1, ref T2 c2, ref T3 c3);

public delegate void QueryCallback<T1, T2, T3, T4>(
    EntityId entity,
    ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);
/*

public sealed class QueryDescription
{
    internal readonly List<Type> All = [];
    internal readonly List<Type> None = [];

    public QueryDescription WithAll<T1>()
        => Add(All, typeof(T1));
    public QueryDescription WithAll<T1, T2>()
        => Add(All, typeof(T1), typeof(T2));
    public QueryDescription WithAll<T1, T2, T3>()
        => Add(All, typeof(T1), typeof(T2), typeof(T3));
    public QueryDescription WithAll<T1, T2, T3, T4>()
        => Add(All, typeof(T1), typeof(T2), typeof(T3), typeof(T4));

    public QueryDescription WithNone<T1>()
        => Add(None, typeof(T1));
    public QueryDescription WithNone<T1, T2>()
        => Add(None, typeof(T1), typeof(T2));
    public QueryDescription WithNone<T1, T2, T3>()
        => Add(None, typeof(T1), typeof(T2), typeof(T3));

    private QueryDescription Add(List<Type> list, params Type[] types)
    {
        list.AddRange(types);
        return this;
    }
}

// One per arity — these live in the Ecs namespace alongside World
public delegate void QueryCallback<T1>(
    EntityId entity, ref T1 c1);

public delegate void QueryCallback<T1, T2>(
    EntityId entity, ref T1 c1, ref T2 c2);

public delegate void QueryCallback<T1, T2, T3>(
    EntityId entity, ref T1 c1, ref T2 c2, ref T3 c3);

public delegate void QueryCallback<T1, T2, T3, T4>(
    EntityId entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);
*/