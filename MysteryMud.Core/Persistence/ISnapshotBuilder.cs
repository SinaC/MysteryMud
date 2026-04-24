using TinyECS;
using MysteryMud.Core.Persistence.Snapshots;

namespace MysteryMud.Core.Persistence;

/// <summary>
/// Reads ECS components from a player entity and produces a PlayerSnapshot.
/// Implement this in your game layer where you have access to all component types.
/// The persistence layer only depends on this interface.
/// </summary>
public interface ISnapshotBuilder
{
    /// <summary>
    /// Build a complete PlayerSnapshot from the entity's current components.
    /// Called from the PersistenceSystem on the game thread (inside the ECS tick),
    /// so it is safe to read components directly.
    /// </summary>
    PlayerSnapshot Build(World world, EntityId entity, long currentTick);
}

/// <summary>
/// Restores ECS components onto a freshly spawned entity from a PlayerSnapshot.
/// </summary>
public interface ISnapshotRestorer
{
    /// <summary>
    /// Apply a loaded snapshot to an entity.
    /// Called during login, before the entity is added to any room.
    /// </summary>
    void Restore(World world, EntityId entity, PlayerSnapshot snapshot, long currentTick);
}
