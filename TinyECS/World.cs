namespace TinyECS;

/// <summary>
/// Central registry.
///
/// Responsibilities:
///   • Create / Destroy entities (with generation tracking)
///   • Own all ComponentStores (one per type)
///   • Provide a thin facade so callers don't juggle stores manually
/// </summary>
public sealed class World
{
    // -------------------------------------------------------------------------
    // EntityId management
    // -------------------------------------------------------------------------

    // Slot = (current generation, alive flag)
    private readonly record struct Slot(uint Generation, bool Alive);

    private Slot[] _slots = new Slot[64];
    private int _nextIndex;                     // first never-used slot
    private readonly Stack<uint> _freeList = new(); // recycled slots

    // -------------------------------------------------------------------------
    // Component store registry
    // -------------------------------------------------------------------------

    // Type → ComponentStore<T> (boxed, retrieved via GetStore<T>)
    private readonly Dictionary<Type, object> _stores = new();

    // -------------------------------------------------------------------------
    // EntityId lifecycle
    // -------------------------------------------------------------------------

    /// <summary>Allocates a new entity, recycling a freed slot when available.</summary>
    public EntityId CreateEntity()
    {
        uint index;
        uint generation;

        if (_freeList.TryPop(out uint recycled))
        {
            index = recycled;
            generation = _slots[index].Generation; // already incremented on Destroy
        }
        else
        {
            index = (uint)_nextIndex++;
            EnsureSlotCapacity(index);
            generation = 1; // generation 0 == invalid, so live entities start at 1
        }

        _slots[index] = new Slot(generation, Alive: true);
        return new EntityId(index, generation);
    }

    /// <summary>
    /// Destroys an entity: bumps its generation (invalidating all existing handles)
    /// and strips all its components.
    /// </summary>
    public void DestroyEntity(EntityId entity)
    {
        if (!IsAlive(entity))
            throw new InvalidOperationException($"Cannot destroy {entity}: not alive.");

        // Remove from every store — iterate over all registered stores.
        foreach (var store in _stores.Values)
            ((IComponentStore)store).RemoveIfPresent(entity);

        uint index = entity.Index;
        _slots[index] = new Slot(Generation: entity.Generation + 1, Alive: false);
        _freeList.Push(index);
    }

    /// <summary>Returns true only if the EntityId was created by this world and has not been destroyed.</summary>
    public bool IsAlive(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _slots.Length) return false;
        ref Slot slot = ref _slots[idx];
        return slot.Alive && slot.Generation == entity.Generation;
    }

    public void Shutdown()
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = new Slot(0, false);
        _freeList.Clear();
        _stores.Clear();
    }

    // -------------------------------------------------------------------------
    // Component access facade
    // -------------------------------------------------------------------------

    public ComponentStore<T> Store<T>()
    {
        var type = typeof(T);
        if (!_stores.TryGetValue(type, out var raw))
        {
            raw = new ComponentStore<T>();
            _stores[type] = raw;
        }
        return (ComponentStore<T>)raw;
    }

    /// <summary>Shorthand: add a component (throws if present).</summary>
    public ref T Add<T>(EntityId entity, T value = default!)
    {
        AssertAlive(entity);
        return ref Store<T>().Add(entity, value);
    }

    /// <summary>Shorthand: add / overwrite a component.</summary>
    public ref T Set<T>(EntityId entity, T value = default!)
    {
        AssertAlive(entity);
        return ref Store<T>().Set(entity, value);
    }

    /// <summary>Shorthand: remove a component (no-op if not present).</summary>
    public void Remove<T>(EntityId entity) => Store<T>().Remove(entity);

    /// <summary>Shorthand: does the EntityId have this component?</summary>
    public bool Has<T>(EntityId entity) => Store<T>().Has(entity);

    /// <summary>Shorthand: get ref to component (throws if absent).</summary>
    public ref T Get<T>(EntityId entity) => ref Store<T>().Get(entity);

    /// <summary>Shorthand: try-get.</summary>
    public bool TryGet<T>(EntityId entity, out T value) => Store<T>().TryGet(entity, out value);

    /// <summary>
    /// Returns a ref to the component, without copying, with a fast existence check.
    /// When <paramref name="exists"/> is false the ref MUST NOT be dereferenced.
    /// </summary>
    public ref T TryGetRef<T>(in EntityId entity, out bool exists)
        => ref Store<T>().TryGetRef(in entity, out exists);

    /// <summary>
    /// Returns all components currently attached to <paramref name="entity"/>,
    /// each boxed as <c>object</c>, paired with its <see cref="Type"/>.
    /// Only stores that have already been touched (via <see cref="Store{T}"/> or
    /// any Set/Has/Get call) are considered — types that were never registered
    /// simply won't appear.
    ///
    /// Primarily useful for debugging, serialisation, or editor tooling.
    /// Avoid calling this in hot tick-loop code (boxing + allocation).
    /// </summary>
    public IEnumerable<(Type Type, object Value)> GetAllComponents(EntityId entity)
    {
        AssertAlive(entity);
        foreach (var (type, store) in _stores)
        {
            object? boxed = ((IComponentStore)store).GetBoxed(entity);
            if (boxed is not null)
                yield return (type, boxed);
        }
    }


    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void AssertAlive(EntityId entity)
    {
        if (!IsAlive(entity))
            throw new InvalidOperationException($"{entity} is not alive.");
    }

    private void EnsureSlotCapacity(uint index)
    {
        if (index >= _slots.Length)
            Array.Resize(ref _slots, Math.Max(_slots.Length * 2, (int)index + 1));
    }

    // -------------------------------------------------------------------------
    // Internal helpers used by Query
    // -------------------------------------------------------------------------

    internal int StoreCount(Type type)
        => _stores.TryGetValue(type, out var s) ? ((ICountable)s).Count : 0;

    internal ReadOnlySpan<EntityId> StoreEntities(Type type)
        => _stores.TryGetValue(type, out var s) ? ((ICountable)s).Entities : ReadOnlySpan<EntityId>.Empty;

    internal bool StoreHas(Type type, EntityId entity)
        => _stores.TryGetValue(type, out var s) && ((IComponentStore)s).Has(entity);

    /// <summary>
    /// Returns the IComponentStore for <paramref name="type"/>, creating an
    /// empty store if it has never been touched.  Used by WorldQueryExtensions
    /// to resolve WithNone types without knowing T at compile time.
    /// </summary>
    internal IComponentStore StoreByType(Type type)
    {
        if (!_stores.TryGetValue(type, out var raw))
        {
            // Create a ComponentStore<T> for the unknown type via reflection.
            // This path is only hit once per type (subsequent calls hit the dict).
            var storeType = typeof(ComponentStore<>).MakeGenericType(type);
            raw = Activator.CreateInstance(storeType)!;
            _stores[type] = raw;
        }
        return (IComponentStore)raw;
    }

    internal interface ICountable
    {
        int Count { get; }
        ReadOnlySpan<EntityId> Entities { get; }
    }

    // -------------------------------------------------------------------------
    // Internal methods only used by unit tests (I know this is crappy)
    // -------------------------------------------------------------------------
    internal object AddRawComponent(EntityId entity, object component = default!)
    {
        AssertAlive(entity);

        var type = component.GetType();

        if (!_stores.TryGetValue(type, out var raw))
        {
            var storeType = typeof(ComponentStore<>).MakeGenericType(type);
            raw = Activator.CreateInstance(storeType)!;
            _stores[type] = raw;
        }

        var store = (IComponentStore)raw;
        return store.AddRaw(entity, component);
    }

    // Internal non-generic interface so Destroy can call RemoveIfPresent without knowing T.
    // ComponentStore<T> implements this explicitly so it doesn't pollute its public surface.
    internal interface IComponentStore : ICountable
    {
        object AddRaw(EntityId entity, object value);
        bool Has(EntityId entity);
        void RemoveIfPresent(EntityId entity);
        /// <summary>Returns the component boxed, or null if the EntityId doesn't have it.</summary>
        object? GetBoxed(EntityId entity);
    }
}