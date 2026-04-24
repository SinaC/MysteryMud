namespace TinyECS;

/// <summary>
/// A sparse-set that maps EntityId → component T.
///
/// Layout:
///   _sparse  : EntityIndex → position in _dense / _components  (or NO_ENTRY)
///   _dense   : packed list of EntityIds that own this component
///   _components : parallel packed list of T values
///
/// Add / Remove are O(1).  Iteration over all T is cache-friendly (packed array).
/// </summary>
public sealed class ComponentStore<T> : World.IComponentStore
{
    private const int NO_ENTRY = -1;

    // Maps EntityId *index* to position in the dense array.
    // Indexed by EntityId.Index — grown on demand.
    private int[] _sparse = new int[64];

    // Packed arrays — always the same length.
    private EntityId[] _dense = new EntityId[16];
    private T[] _components = new T[16];
    private int _count;

    // Generations are stored in the dense array (via EntityId) so we can detect
    // stale handles without a separate table.

    public ComponentStore()
    {
        Array.Fill(_sparse, NO_ENTRY);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public int Count => _count;

    /// <summary>Returns true if the EntityId has this component and its generation is current.</summary>
    public bool Has(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _sparse.Length) return false;
        int pos = _sparse[idx];
        return pos != NO_ENTRY && _dense[pos] == entity;
    }

    /// <summary>
    /// Adds the component.  Returns ref to the stored value for
    /// in-place initialisation:  store.Add(e) = new MyComp { ... };
    /// Throws if already present.
    /// </summary>
    public ref T Add(EntityId entity, T value = default!)
    {
        if (Has(entity))
            throw new InvalidOperationException($"{entity} does akready have component {typeof(T).Name}");

        return ref Set(entity, value);
    }

    /// <summary>
    /// Adds or replaces the component.  Returns ref to the stored value for
    /// in-place initialisation:  store.Set(e) = new MyComp { ... };
    /// </summary>
    public ref T Set(EntityId entity, T value = default!)
    {
        EnsureSparseCapacity(entity.Index);

        int pos = _sparse[entity.Index];
        if (pos != NO_ENTRY && _dense[pos] == entity)
        {
            // Already exists — just update.
            _components[pos] = value;
            return ref _components[pos];
        }

        // New entry.
        EnsureDenseCapacity();
        pos = _count++;
        _sparse[entity.Index] = pos;
        _dense[pos] = entity;
        _components[pos] = value;
        return ref _components[pos];
    }

    /// <summary>Gets a reference to the component.  Throws if not present.</summary>
    public ref T Get(EntityId entity)
    {
        int pos = GetPosition(entity);
        return ref _components[pos];
    }

    /// <summary>Try-get pattern without exceptions.</summary>
    public bool TryGet(EntityId entity, out T value)
    {
        uint idx = entity.Index;
        if (idx < _sparse.Length)
        {
            int pos = _sparse[idx];
            if (pos != NO_ENTRY && _dense[pos] == entity)
            {
                value = _components[pos];
                return true;
            }
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Returns a ref to the stored component, avoiding a copy on the happy path.
    /// When <paramref name="exists"/> is false the returned ref is a stable-but-
    /// meaningless sentinel (via <see cref="System.Runtime.InteropServices.MemoryMarshal"/>);
    /// the caller MUST NOT read or write through it in that case.
    ///
    /// Typical usage:
    /// <code>
    ///   ref Health hp = ref store.TryGetRef(entity, out bool ok);
    ///   if (ok) hp.Current -= 5;
    /// </code>
    /// </summary>
    public ref T TryGetRef(in EntityId entity, out bool exists)
    {
        uint idx = entity.Index;
        if (idx < _sparse.Length)
        {
            int pos = _sparse[idx];
            if (pos != NO_ENTRY && _dense[pos] == entity)
            {
                exists = true;
                return ref _components[pos];
            }
        }
        exists = false;
        // A non-null ref is required by the compiler even on the failure path.
        // Unsafe.NullRef<T>() is the canonical "null ref" in C# — it is valid
        // to return but MUST NOT be dereferenced. The caller guards on exists.
        return ref System.Runtime.CompilerServices.Unsafe.NullRef<T>();
    }

    /// <summary>
    /// Removes the component.  Uses the classic sparse-set swap-with-last trick
    /// to keep the dense arrays packed.
    /// </summary>
    public bool Remove(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _sparse.Length) return false;

        int pos = _sparse[idx];
        if (pos == NO_ENTRY || _dense[pos] != entity) return false;

        int last = _count - 1;

        if (pos != last)
        {
            // Swap the removed slot with the last element.
            EntityId lastEntity = _dense[last];
            _dense[pos] = lastEntity;
            _components[pos] = _components[last];
            _sparse[lastEntity.Index] = pos;
        }

        // Clear the last slot.
        _sparse[idx] = NO_ENTRY;
        _dense[last] = EntityId.Invalid;
        _components[last] = default!;
        _count--;
        return true;
    }

    /// <summary>Removes all components for this EntityId (same as Remove but no return value).</summary>
    public void RemoveIfPresent(EntityId entity) => Remove(entity);
    void World.IComponentStore.RemoveIfPresent(EntityId entity) => Remove(entity);
    bool World.IComponentStore.Has(EntityId entity) => Has(entity);
    int World.ICountable.Count => _count;
    ReadOnlySpan<EntityId> World.ICountable.Entities => Entities;
    object? World.IComponentStore.GetBoxed(EntityId entity)
        => TryGet(entity, out T value) ? value : null;

    // -------------------------------------------------------------------------
    // Iteration helpers — expose Span for zero-allocation loops
    // -------------------------------------------------------------------------

    /// <summary>All entities that have this component, in packed (arbitrary) order.</summary>
    public ReadOnlySpan<EntityId> Entities => _dense.AsSpan(0, _count);

    /// <summary>All component values, parallel to Entities.</summary>
    public Span<T> Components => _components.AsSpan(0, _count);

    /// <summary>
    /// Enumerate (entity, ref component) pairs.
    /// Usage:
    ///   foreach (var (e, ref hp) in store.Each()) { ... }
    /// </summary>
    public Enumerator Each() => new(this);

    // -------------------------------------------------------------------------
    // Internal fast-path helpers — used by WorldQueryExtensions
    // -------------------------------------------------------------------------
    internal ref T GetAtDenseIndex(int pos) => ref _components[pos];
    internal int GetDenseIndex(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _sparse.Length) return -1;
        int pos = _sparse[idx];
        return (pos != -1 && _dense[pos] == entity) ? pos : -1;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private int GetPosition(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx < _sparse.Length)
        {
            int pos = _sparse[idx];
            if (pos != NO_ENTRY && _dense[pos] == entity)
                return pos;
        }
        throw new InvalidOperationException($"{entity} does not have component {typeof(T).Name}");
    }

    private void EnsureSparseCapacity(uint index)
    {
        if (index >= _sparse.Length)
        {
            int newSize = Math.Max(_sparse.Length * 2, (int)index + 1);
            int[] newSparse = new int[newSize];
            Array.Fill(newSparse, NO_ENTRY);
            _sparse.AsSpan().CopyTo(newSparse.AsSpan());
            _sparse = newSparse;
        }
    }

    private void EnsureDenseCapacity()
    {
        if (_count == _dense.Length)
        {
            int newSize = _dense.Length * 2;
            Array.Resize(ref _dense, newSize);
            Array.Resize(ref _components, newSize);
        }
    }

    // -------------------------------------------------------------------------
    // Enumerator (ref-returning, no heap allocation)
    // -------------------------------------------------------------------------

    public ref struct Enumerator
    {
        private readonly ComponentStore<T> _store;
        private int _index;

        internal Enumerator(ComponentStore<T> store)
        {
            _store = store;
            _index = -1;
        }

        public bool MoveNext() => ++_index < _store._count;

        public EntityId EntityId => _store._dense[_index];
        public ref T Component => ref _store._components[_index];

        public Enumerator GetEnumerator() => this;
    }
}