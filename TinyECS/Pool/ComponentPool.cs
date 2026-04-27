namespace TinyECS.Pool;

/// <summary>
/// Flat component pool — the alternative to <see cref="ComponentStore{T}"/>.
///
/// LAYOUT
/// ──────
/// Four parallel arrays, all indexed by EntityId.Index:
///
///   _pool           : T[]    — component value at entity index i
///   _generations    : uint[] — generation of the entity that owns slot i, or 0 = empty
///   _entityListPos  : int[]  — position of entity i in the _entities iteration list,
///                              or NO_ENTRY when absent
///
/// Plus one packed iteration list:
///   _entities       : EntityId[] — packed list of entities that own a component,
///                                  used for Count and iteration pivot
///
/// ALL operations are O(1):
///   Has  / Get       — 1 array read (_generations check + _pool access)
///   Set  (new entry) — write to _pool, _generations, _entityListPos; append to _entities
///   Remove           — clear _pool/_generations/_entityListPos; swap-with-last in _entities
///                      and update _entityListPos for the swapped entity
///
/// TRADEOFFS vs ComponentStore
/// ────────────────────────────
/// + Has / Get: O(1), 1 array read (no sparse lookup)
/// + Remove: O(1) via _entityListPos (ComponentStore also O(1) via _sparse)
/// − Memory: allocates worldCapacity slots per component type even for absent entities
/// </summary>
public sealed class ComponentPool<T> : PoolWorld.IComponentPool
{
    private const int NO_ENTRY = -1;

    // Direct-indexed arrays — indexed by EntityId.Index
    private T[] _pool = new T[64];
    private uint[] _generations = new uint[64];   // 0 = slot empty
    private int[] _entityListPos = new int[64];    // position in _entities, or NO_ENTRY

    // Packed entity list for iteration
    private EntityId[] _entities = new EntityId[16];
    private int _count;

    public ComponentPool()
    {
        Array.Fill(_entityListPos, NO_ENTRY);
    }

    // -------------------------------------------------------------------------
    // Public API  (mirrors ComponentStore<T>)
    // -------------------------------------------------------------------------

    public int Count => _count;

    /// <summary>Returns true if the entity currently owns a component in this pool.</summary>
    public bool Has(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _generations.Length) return false;
        return _generations[idx] == entity.Generation;
    }

    /// <summary>Adds or replaces the component value.</summary>
    public ref T Set(EntityId entity, T value = default!)
    {
        uint idx = entity.Index;
        EnsureCapacity(idx);

        if (_generations[idx] == entity.Generation)
        {
            // Already present — update in place, no list change needed.
            _pool[idx] = value;
            return ref _pool[idx];
        }

        // New entry — append to iteration list and record the position.
        _generations[idx] = entity.Generation;
        _pool[idx] = value;

        EnsureEntitiesCapacity();
        _entityListPos[idx] = _count;
        _entities[_count++] = entity;
        return ref _pool[idx];
    }

    /// <summary>Gets a ref to the component.  Throws if absent.</summary>
    public ref T Get(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx < _generations.Length && _generations[idx] == entity.Generation)
            return ref _pool[idx];
        throw new InvalidOperationException($"{entity} does not have component {typeof(T).Name}");
    }

    /// <summary>Try-get without exceptions.</summary>
    public bool TryGet(EntityId entity, out T value)
    {
        uint idx = entity.Index;
        if (idx < _generations.Length && _generations[idx] == entity.Generation)
        {
            value = _pool[idx];
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Returns a ref to the component without copying.
    /// When <paramref name="exists"/> is false the ref MUST NOT be dereferenced.
    /// </summary>
    public ref T TryGetRef(in EntityId entity, out bool exists)
    {
        uint idx = entity.Index;
        if (idx < _generations.Length && _generations[idx] == entity.Generation)
        {
            exists = true;
            return ref _pool[idx];
        }
        exists = false;
        return ref System.Runtime.CompilerServices.Unsafe.NullRef<T>();
    }

    /// <summary>
    /// Removes the component.  O(1): uses _entityListPos to find the entity's
    /// position in _entities directly, then swap-with-last.
    /// </summary>
    public bool Remove(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _generations.Length || _generations[idx] != entity.Generation)
            return false;

        // Clear the pool slot.
        _generations[idx] = 0;
        _pool[idx] = default!;

        // O(1) removal from the packed entity list.
        int pos = _entityListPos[idx];
        int last = _count - 1;

        if (pos != last)
        {
            // Swap last entity into the vacated position.
            EntityId swapped = _entities[last];
            _entities[pos] = swapped;
            _entityListPos[swapped.Index] = pos;   // update reverse index for swapped entity
        }

        _entities[last] = EntityId.Invalid;
        _entityListPos[idx] = NO_ENTRY;
        _count--;
        return true;
    }

    public void RemoveIfPresent(EntityId entity) => Remove(entity);

    // -------------------------------------------------------------------------
    // Iteration helpers
    // -------------------------------------------------------------------------

    /// <summary>All entities that currently own this component.</summary>
    public ReadOnlySpan<EntityId> Entities => _entities.AsSpan(0, _count);

    /// <summary>Enumerator for (entity, ref component) pairs.</summary>
    public Enumerator Each() => new(this);

    // -------------------------------------------------------------------------
    // Internal fast-path helpers — used by WorldPoolQueryExtensions
    //
    // NOTE: in a ComponentPool the "dense index" IS the entity's raw Index,
    // not a position in a packed array.  GetAtIndex takes an entity index and
    // returns ref _pool[entityIndex] — one array read, zero indirection.
    // -------------------------------------------------------------------------

    /// <summary>Entity at position <paramref name="pos"/> in the iteration list.</summary>
    internal EntityId EntityAt(int pos) => _entities[pos];

    /// <summary>
    /// Returns the entity index (== pool slot) for <paramref name="entity"/>,
    /// or -1 if absent.  Full generation check.
    /// </summary>
    internal int GetEntityIndex(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _generations.Length) return -1;
        return _generations[idx] == entity.Generation ? (int)idx : -1;
    }

    /// <summary>
    /// Returns the entity index without re-checking the generation.
    /// Safe only when the entity is known live (came from iterating
    /// another pool's entity list).
    /// </summary>
    internal int GetEntityIndexFast(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _generations.Length || _generations[idx] == 0) return -1;
        return (int)idx;
    }

    /// <summary>
    /// Returns a ref to the component for the given entity index.
    /// The caller must have validated via GetEntityIndex / GetEntityIndexFast first.
    /// This is the hot-path accessor — one array read, no indirection.
    /// </summary>
    internal ref T GetAtIndex(int entityIndex) => ref _pool[entityIndex];

    // -------------------------------------------------------------------------
    // PoolWorld.IComponentPool explicit implementations
    // -------------------------------------------------------------------------

    void PoolWorld.IComponentPool.RemoveIfPresent(EntityId entity) => Remove(entity);
    bool PoolWorld.IComponentPool.Has(EntityId entity) => Has(entity);
    int PoolWorld.ICountable.Count => _count;
    ReadOnlySpan<EntityId> PoolWorld.ICountable.Entities => Entities;
    object? PoolWorld.IComponentPool.GetBoxed(EntityId entity)
        => TryGet(entity, out T value) ? value : null;

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void EnsureCapacity(uint index)
    {
        if (index < (uint)_pool.Length) return;

        int newSize = Math.Max(_pool.Length * 2, (int)index + 1);
        int oldSize = _pool.Length;

        Array.Resize(ref _pool, newSize);
        Array.Resize(ref _generations, newSize);
        Array.Resize(ref _entityListPos, newSize);

        // Fill the newly added slots with NO_ENTRY.
        _entityListPos.AsSpan(oldSize, newSize - oldSize).Fill(NO_ENTRY);
    }

    private void EnsureEntitiesCapacity()
    {
        if (_count < _entities.Length) return;
        Array.Resize(ref _entities, _entities.Length * 2);
    }

    // -------------------------------------------------------------------------
    // Enumerator
    // -------------------------------------------------------------------------

    public ref struct Enumerator
    {
        private readonly ComponentPool<T> _pool;
        private int _index;

        internal Enumerator(ComponentPool<T> pool) { _pool = pool; _index = -1; }

        public bool MoveNext() => ++_index < _pool._count;

        public EntityId Entity => _pool._entities[_index];
        public ref T Component => ref _pool._pool[_pool._entities[_index].Index];

        public Enumerator GetEnumerator() => this;
    }
}