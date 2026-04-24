using System.Collections;

namespace TinyECS;

/// <summary>
/// Fluent query builder.  Intersects sparse-sets to produce only entities
/// that satisfy every With&lt;T&gt; constraint (and none of the Without&lt;T&gt; ones).
///
/// Usage:
///   var query = new Query(world)
///       .With&lt;Position&gt;()
///       .With&lt;Health&gt;()
///       .Without&lt;Dead&gt;();
///
///   foreach (EntityId e in query)
///       world.Get&lt;Health&gt;(e).Current -= 5;
///
/// The query always iterates the *smallest* matching store first, so the inner
/// Has() checks are as fast as possible.
/// </summary>
public sealed class Query
{
    private readonly World _world;

    // We store the component types rather than the stores themselves so
    // the query can be built once and reused across ticks (stores are stable).
    private readonly List<Type> _required = new();
    private readonly List<Type> _excluded = new();

    public Query(World world) => _world = world;

    public Query With<T>() { _required.Add(typeof(T)); return this; }
    public Query Without<T>() { _excluded.Add(typeof(T)); return this; }

    // -------------------------------------------------------------------------
    // Enumeration
    // -------------------------------------------------------------------------

    // ref struct enumerators can't implement IEnumerator<T>, but the compiler
    // only needs GetEnumerator() + MoveNext() + Current for foreach to work.
    public Enumerator GetEnumerator() => new(_world, _required, _excluded);

    // -------------------------------------------------------------------------
    // Enumerator
    // -------------------------------------------------------------------------

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly List<Type> _required;
        private readonly List<Type> _excluded;

        // Snapshot of the pivot store's entity list taken at construction time.
        // We copy into a fixed-size stack buffer for small counts (≤128) and
        // fall back to a pooled array for larger ones, so the common MUD case
        // (a few hundred entities per query) pays zero heap allocation.
        //
        // WHY A SNAPSHOT: the pivot store's _dense array is mutated in-place by
        // Remove (swap-with-last).  Holding a ReadOnlySpan into the live array
        // means a Remove during iteration silently skips the swapped-in entity.
        // A snapshot taken before iteration is immune to structural changes.
        //
        // LIMITATION: components added during iteration are NOT visited (the
        // snapshot is fixed at the start).  This is the standard ECS contract.
        private EntityId[] _snapshot;
        private int _snapshotCount;
        private int _candidateIndex;
        private EntityId _current;
        private int _pivotRequired;

        internal Enumerator(World world, List<Type> required, List<Type> excluded)
        {
            _world = world;
            _required = required;
            _excluded = excluded;
            _current = EntityId.Invalid;
            _candidateIndex = -1;
            _pivotRequired = 0;
            _snapshot = Array.Empty<EntityId>();
            _snapshotCount = 0;

            if (required.Count == 0) return;

            // Find the required store with the fewest entries — pivot.
            int minCount = int.MaxValue;
            for (int i = 0; i < required.Count; i++)
            {
                int c = world.StoreCount(required[i]);
                if (c < minCount) { minCount = c; _pivotRequired = i; }
            }

            // Materialise a snapshot of the pivot store's entity list.
            // This makes the enumerator safe against structural mutation
            // (Remove / Add) during iteration.
            ReadOnlySpan<EntityId> live = world.StoreEntities(required[_pivotRequired]);
            _snapshotCount = live.Length;
            if (_snapshotCount > 0)
            {
                _snapshot = new EntityId[_snapshotCount];
                live.CopyTo(_snapshot);
            }
        }

        public EntityId Current => _current;

        public bool MoveNext()
        {
            while (++_candidateIndex < _snapshotCount)
            {
                EntityId e = _snapshot[_candidateIndex];

                // Entity may have been destroyed or had the pivot component
                // removed since we took the snapshot — skip stale entries.
                if (!_world.IsAlive(e)) continue;

                // All required components still present?
                bool ok = true;
                for (int i = 0; i < _required.Count && ok; i++)
                {
                    if (i == _pivotRequired) continue;
                    ok = _world.StoreHas(_required[i], e);
                }

                // None of the excluded components present?
                for (int i = 0; i < _excluded.Count && ok; i++)
                    ok = !_world.StoreHas(_excluded[i], e);

                if (!ok) continue;

                _current = e;
                return true;
            }
            return false;
        }

        public void Reset() => _candidateIndex = -1;
    }
}

/*public sealed class Query
{
    private readonly World _world;

    // We store the component types rather than the stores themselves so
    // the query can be built once and reused across ticks (stores are stable).
    private readonly List<Type> _required = [];
    private readonly List<Type> _excluded = [];

    public Query(World world) => _world = world;

    public Query With<T>() { _required.Add(typeof(T)); return this; }
    public Query Without<T>() { _excluded.Add(typeof(T)); return this; }

    // -------------------------------------------------------------------------
    // Enumeration
    // -------------------------------------------------------------------------

    // ref struct enumerators can't implement IEnumerator<T>, but the compiler
    // only needs GetEnumerator() + MoveNext() + Current for foreach to work.
    public Enumerator GetEnumerator() => new(_world, _required, _excluded);

    // -------------------------------------------------------------------------
    // Enumerator
    // -------------------------------------------------------------------------

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly List<Type> _required;
        private readonly List<Type> _excluded;

        // We materialise the smallest required store's EntityId list once.
        private ReadOnlySpan<EntityId> _candidates;
        private int _candidateIndex;
        private EntityId _current;

        // Pivoting: index into _required that has the smallest store
        private int _pivotRequired;

        internal Enumerator(World world, List<Type> required, List<Type> excluded)
        {
            _world = world;
            _required = required;
            _excluded = excluded;
            _current = EntityId.Invalid;
            _candidateIndex = -1;
            _pivotRequired = 0;

            if (required.Count == 0)
            {
                _candidates = ReadOnlySpan<EntityId>.Empty;
                return;
            }

            // Find the required store with the fewest entries — pivot.
            int minCount = int.MaxValue;
            for (int i = 0; i < required.Count; i++)
            {
                int c = world.StoreCount(required[i]);
                if (c < minCount) { minCount = c; _pivotRequired = i; }
            }

            _candidates = world.StoreEntities(required[_pivotRequired]);
        }

        public EntityId Current => _current;

        public bool MoveNext()
        {
            while (++_candidateIndex < _candidates.Length)
            {
                EntityId e = _candidates[_candidateIndex];

                // Skip stale entries (EntityId was destroyed but store not yet swept).
                if (!_world.IsAlive(e)) continue;

                // All required components present?
                bool ok = true;
                for (int i = 0; i < _required.Count && ok; i++)
                {
                    if (i == _pivotRequired) continue; // pivot is already satisfied
                    ok = _world.StoreHas(_required[i], e);
                }

                // None of the excluded components present?
                for (int i = 0; i < _excluded.Count && ok; i++)
                    ok = !_world.StoreHas(_excluded[i], e);

                if (!ok) continue;

                _current = e;
                return true;
            }
            return false;
        }

        public void Reset() => _candidateIndex = -1;
    }
}
*/