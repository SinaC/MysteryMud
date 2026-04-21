using Arch.Core;

namespace MysteryMud.Core.Persistence;

public interface IDirtyTracker
{
    void MarkDirty(Entity entity, DirtyReason reason);
    void MarkForDisconnect(Entity entity);

    IReadOnlyList<DirtyEntry> Drain(Func<DirtyEntry, bool>? filter = null);
    DirtyEntry? DrainEntity(Entity entity);

    bool IsDirty(Entity entity);
    int Count { get; }
}
