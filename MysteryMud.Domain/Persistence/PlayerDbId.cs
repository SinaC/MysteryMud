namespace MysteryMud.Domain.Persistence;

// ── Marker components for DB ids ─────────────────────────────────────

/// <summary>Stores the DB row id on a player entity after load/save.</summary>
record struct PlayerDbId(long Value);
