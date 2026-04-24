using TinyECS;

namespace MysteryMud.Core.Persistence;

// ─────────────────────────────────────────────────────────────
//  Per-entity dirty entry
// ─────────────────────────────────────────────────────────────

public sealed class DirtyEntry
{
    public EntityId Entity { get; }
    public DirtyReason Reasons { get; private set; }
    public DateTime FirstMark { get; }
    public DateTime LastMark { get; private set; }

    public DirtyEntry(EntityId entity, DirtyReason reason)
    {
        Entity = entity;
        Reasons = reason;
        FirstMark = DateTime.UtcNow;
        LastMark = FirstMark;
    }

    /// <summary>Merge additional reasons into this entry (lock held by caller).</summary>
    public void Accumulate(DirtyReason reason)
    {
        Reasons |= reason;
        LastMark = DateTime.UtcNow;
    }

    public bool Has(DirtyReason reason) => (Reasons & reason) != 0;
}
