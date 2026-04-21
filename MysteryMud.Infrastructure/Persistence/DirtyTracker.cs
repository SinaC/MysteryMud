using Arch.Core;
using MysteryMud.Core.Persistence;
using System.Collections.Concurrent;

namespace MysteryMud.Infrastructure.Persistence;

// ─────────────────────────────────────────────────────────────
//  DirtyTracker
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Thread-safe registry of entities that need to be persisted.
/// Game systems call MarkDirty; PersistenceSystem drains the queue.
/// </summary>
public sealed class DirtyTracker : IDirtyTracker
{
    // entity id → dirty entry
    private readonly ConcurrentDictionary<int, DirtyEntry> _dirty = new();

    // ── Public API ───────────────────────────────────────────

    /// <summary>Mark an entity dirty with one or more reasons.</summary>
    public void MarkDirty(Entity entity, DirtyReason reason)
    {
        _dirty.AddOrUpdate(
            entity.Id,
            _ => new DirtyEntry(entity, reason),
            (_, existing) => { existing.Accumulate(reason); return existing; });
    }

    /// <summary>
    /// Mark an entity for immediate disconnect-save.
    /// Adds all reasons so nothing is skipped.
    /// </summary>
    public void MarkForDisconnect(Entity entity)
        => MarkDirty(entity, DirtyReason.All);

    /// <summary>
    /// Drain all entries that match the filter predicate.
    /// Typical use: flush everything, or flush only non-cooldown reasons.
    /// </summary>
    public IReadOnlyList<DirtyEntry> Drain(Func<DirtyEntry, bool>? filter = null)
    {
        var result = new List<DirtyEntry>();

        foreach (var key in _dirty.Keys.ToArray())
        {
            if (_dirty.TryGetValue(key, out var entry) && (filter is null || filter(entry)))
            {
                if (_dirty.TryRemove(key, out _))
                    result.Add(entry);
            }
        }

        return result;
    }

    /// <summary>
    /// Drain a single entity (used on disconnect to force an immediate save).
    /// </summary>
    public DirtyEntry? DrainEntity(Entity entity)
    {
        _dirty.TryRemove(entity.Id, out var entry);
        return entry;
    }

    public bool IsDirty(Entity entity) => _dirty.ContainsKey(entity.Id);

    public int Count => _dirty.Count;
}
