using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Persistence;
using TinyECS;

namespace MysteryMud.Domain.Systems;

/// <summary>
/// ECS system responsible for draining the DirtyTracker and writing to the DB.
///
/// Three save triggers:
///   1. Critical           - flushed immediately regardless of player count
///   2. Pressure           - enough dirty players to justify a batch write
///   3. Periodic autosave  — flushes entries every AutosaveInterval
///   4. Disconnect         — immediate flush via SaveOnDisconnectAsync (called externally)
///
/// The system must be updated once per tick by your ECS scheduler.
/// It is intentionally NOT an Arch ISystem to avoid pulling in scheduler coupling —
/// call Update() from wherever you drive your systems.
/// </summary>
public sealed class PersistenceSystem
{
    // ── Configuration ────────────────────────────────────────

    /// <summary>How often the autosave flush runs regardless of batch size.</summary>
    public TimeSpan AutosaveInterval { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Flush immediately if this many dirty entries accumulate
    /// (significant-event pressure valve).
    /// </summary>
    public int ImmediateFlushThreshold { get; init; } = 20;

    // ── Dependencies ─────────────────────────────────────────

    private readonly World _world;
    private readonly IDirtyTracker _tracker;
    private readonly IPersistenceService _persistence;
    private readonly ISnapshotBuilder _builder;
    private readonly ILogger _log;

    // ── State ────────────────────────────────────────────────

    private DateTime _lastAutosave = DateTime.UtcNow;

    public PersistenceSystem(
        World world,
        IDirtyTracker tracker,
        IPersistenceService persistence,
        ISnapshotBuilder builder,
        ILogger log)
    {
        _world = world;
        _tracker = tracker;
        _persistence = persistence;
        _builder = builder;
        _log = log;
    }

    // ─────────────────────────────────────────────────────────
    //  Called once per tick by your game loop
    // ─────────────────────────────────────────────────────────

    public void Update(GameState state)
    {
        var now = DateTime.UtcNow;

        // Tier 1 — critical: flush immediately regardless of player count
        if (_tracker.HasCritical)
        {
            var critical = _tracker.Drain(e => e.Has(DirtyReason.Critical));
            if (critical.Count > 0)
            {
                _ = FlushBatchAsync(critical, state.CurrentTick);
                return; // let non-critical wait for next tier
            }
        }

        // Tier 2 — pressure: enough dirty players to justify a batch write
        var pressureFlush = _tracker.Count >= ImmediateFlushThreshold;

        // Tier 3 — autosave: time-based fallback for low player count servers
        var autosaveDue = (now - _lastAutosave) >= AutosaveInterval;

        if (!pressureFlush && !autosaveDue) return;

        var batch = _tracker.Drain();

        if (batch.Count == 0) return;

        if (autosaveDue) _lastAutosave = now;

        _ = FlushBatchAsync(batch, state.CurrentTick);
    }

    // ─────────────────────────────────────────────────────────
    //  External call: immediate save on disconnect
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the connection handler when a player disconnects cleanly.
    /// Forces a save even if the entity isn't currently dirty.
    /// </summary>
    public async Task SaveOnDisconnectAsync(EntityId entity, long currentTick)
    {
        // Remove from dirty queue (we're saving now)
        _tracker.DrainEntity(entity);

        await SaveEntityAsync(entity, currentTick);
    }

    // ─────────────────────────────────────────────────────────
    //  Shutdown: drain everything
    // ─────────────────────────────────────────────────────────

    public async Task FlushAllAsync(long currentTick)
    {
        var all = _tracker.Drain();
        if (all.Count > 0)
            await FlushBatchAsync(all, currentTick);
    }

    // ─────────────────────────────────────────────────────────
    //  Internals
    // ─────────────────────────────────────────────────────────

    private async Task FlushBatchAsync(IReadOnlyList<DirtyEntry> batch, long tick)
    {
        _log.LogDebug("Persistence: flushing {Count} dirty entities", batch.Count);

        // Saves run sequentially to avoid SQLite write contention.
        // If you ever switch to PostgreSQL, parallelise here with SemaphoreSlim.
        foreach (var entry in batch)
        {
            try
            {
                await SaveEntityAsync(entry.Entity, tick);
            }
            catch (Exception ex)
            {
                _log.LogError(ex,
                    "Persistence: failed to save entity {EntityId} (reasons: {Reasons})",
                    entry.Entity.Index, entry.Reasons);
                // Re-mark dirty so we retry next cycle
                _tracker.MarkDirty(entry.Entity, entry.Reasons);
            }
        }
    }

    private async Task SaveEntityAsync(EntityId entity, long tick)
    {
        // Build snapshot on the calling thread.
        // If this is called from Update() it's already on the game thread.
        // If called from SaveOnDisconnectAsync (async context) the caller must
        // ensure the entity is still alive and not being modified concurrently.
        if (!_world.IsAlive(entity))
        {
            _log.LogWarning("Persistence: skipping dead entity {EntityId}", entity.Index);
            return;
        }

        var snapshot = _builder.Build(_world, entity, tick);
        await _persistence.SavePlayerAsync(snapshot);

        _log.LogDebug("Persistence: saved player '{Name}'", snapshot.Name);
    }
}
