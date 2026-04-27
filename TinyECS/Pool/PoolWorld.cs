namespace TinyECS.Pool;

/// <summary>
/// Identical public API to <see cref="World"/>, but uses
/// <see cref="ComponentPool{T}"/> (flat direct-indexed arrays) instead of
/// <see cref="ComponentStore{T}"/> (sparse-set) for component storage.
///
/// Drop-in replacement for benchmarking and comparison.  Every method
/// delegates to the same logic as World; the only difference is the
/// concrete store type returned by <see cref="Pool{T}"/>.
/// </summary>
public sealed class PoolWorld
{
    // -------------------------------------------------------------------------
    // Entity management  (identical to World)
    // -------------------------------------------------------------------------

    private readonly record struct Slot(uint Generation, bool Alive);

    private Slot[] _slots = new Slot[64];
    private int _nextIndex;
    private readonly Stack<uint> _freeList = new();

    private readonly Dictionary<Type, object> _pools = new();

    // -------------------------------------------------------------------------
    // Entity lifecycle
    // -------------------------------------------------------------------------

    public EntityId CreateEntity()
    {
        uint index;
        uint generation;

        if (_freeList.TryPop(out uint recycled))
        {
            index = recycled;
            generation = _slots[index].Generation;
        }
        else
        {
            index = (uint)_nextIndex++;
            EnsureSlotCapacity(index);
            generation = 1;
        }

        _slots[index] = new Slot(generation, Alive: true);
        return new EntityId(index, generation);
    }

    public void DestroyEntity(EntityId entity)
    {
        if (!IsAlive(entity))
            throw new InvalidOperationException($"Cannot destroy {entity}: not alive.");

        foreach (var pool in _pools.Values)
            ((IComponentPool)pool).RemoveIfPresent(entity);

        uint index = entity.Index;
        _slots[index] = new Slot(Generation: entity.Generation + 1, Alive: false);
        _freeList.Push(index);
    }

    public bool IsAlive(EntityId entity)
    {
        uint idx = entity.Index;
        if (idx >= _slots.Length) return false;
        ref Slot slot = ref _slots[idx];
        return slot.Alive && slot.Generation == entity.Generation;
    }

    // -------------------------------------------------------------------------
    // Component access facade
    // -------------------------------------------------------------------------

    public ComponentPool<T> Pool<T>()
    {
        var type = typeof(T);
        if (!_pools.TryGetValue(type, out var raw))
        {
            raw = new ComponentPool<T>();
            _pools[type] = raw;
        }
        return (ComponentPool<T>)raw;
    }

    public ref T Set<T>(EntityId entity, T value = default!)
    {
        AssertAlive(entity);
        return ref Pool<T>().Set(entity, value);
    }

    public void Remove<T>(EntityId entity) => Pool<T>().Remove(entity);

    public bool Has<T>(EntityId entity) => Pool<T>().Has(entity);

    public ref T Get<T>(EntityId entity) => ref Pool<T>().Get(entity);

    public bool TryGet<T>(EntityId entity, out T value) => Pool<T>().TryGet(entity, out value);

    public ref T TryGetRef<T>(in EntityId entity, out bool exists)
        => ref Pool<T>().TryGetRef(in entity, out exists);

    public IEnumerable<(Type Type, object Value)> GetAllComponents(EntityId entity)
    {
        AssertAlive(entity);
        foreach (var (type, pool) in _pools)
        {
            object? boxed = ((IComponentPool)pool).GetBoxed(entity);
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
    // Internal helpers used by WorldPoolQueryExtensions
    // -------------------------------------------------------------------------

    internal int PoolCount(Type type)
        => _pools.TryGetValue(type, out var p) ? ((ICountable)p).Count : 0;

    internal ReadOnlySpan<EntityId> PoolEntities(Type type)
        => _pools.TryGetValue(type, out var p) ? ((ICountable)p).Entities : ReadOnlySpan<EntityId>.Empty;

    internal bool PoolHas(Type type, EntityId entity)
        => _pools.TryGetValue(type, out var p) && ((IComponentPool)p).Has(entity);

    internal IComponentPool PoolByType(Type type)
    {
        if (!_pools.TryGetValue(type, out var raw))
        {
            var poolType = typeof(ComponentPool<>).MakeGenericType(type);
            raw = Activator.CreateInstance(poolType)!;
            _pools[type] = raw;
        }
        return (IComponentPool)raw;
    }

    // -------------------------------------------------------------------------
    // Internal interfaces
    // -------------------------------------------------------------------------

    internal interface ICountable
    {
        int Count { get; }
        ReadOnlySpan<EntityId> Entities { get; }
    }

    internal interface IComponentPool : ICountable
    {
        bool Has(EntityId entity);
        void RemoveIfPresent(EntityId entity);
        object? GetBoxed(EntityId entity);
    }
}
