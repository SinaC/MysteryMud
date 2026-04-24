using TinyECS;

namespace MysteryMud.Core.Persistence;

public interface IDirtyTracker
{
    void MarkDirty(EntityId entity, DirtyReason reason);
    void MarkForDisconnect(EntityId entity);

    IReadOnlyList<DirtyEntry> Drain(Func<DirtyEntry, bool>? filter = null);
    DirtyEntry? DrainEntity(EntityId entity);

    bool IsDirty(EntityId entity);

    bool HasCritical { get; }
    int Count { get; }
}
