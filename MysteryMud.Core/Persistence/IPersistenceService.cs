using MysteryMud.Core.Persistence.Snapshots;

namespace MysteryMud.Core.Persistence;

public interface IPersistenceService
{
    // ── Players ──────────────────────────────────────────────
    Task<long> SavePlayerAsync(PlayerSnapshot snapshot, CancellationToken ct = default);
    Task<PlayerSnapshot?> LoadPlayerAsync(string name, CancellationToken ct = default);
    Task DeletePlayerAsync(long playerId, CancellationToken ct = default);

    // ── Existence check (login gate) ─────────────────────────
    Task<bool> PlayerExistsAsync(string name, CancellationToken ct = default);
}
