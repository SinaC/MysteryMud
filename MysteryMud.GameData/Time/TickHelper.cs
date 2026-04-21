namespace MysteryMud.GameData.Time;

/// <summary>
/// Helpers for converting between absolute tick values (in-memory)
/// and relative tick offsets (persisted).
///
/// Save:  offset = absoluteTick - currentTick   (can be negative if already past)
/// Load:  absoluteTick = currentTick + offset   (clamp to currentTick if negative)
/// </summary>
public static class TickHelper
{
    public const long Permanent = -1L;

    // ── Save helpers ─────────────────────────────────────────

    /// <summary>
    /// Convert an absolute expiration tick to a remaining offset.
    /// Returns Permanent (-1) if the input is Permanent.
    /// </summary>
    public static long ToExpirationOffset(long absoluteTick, long currentTick)
        => absoluteTick == Permanent ? Permanent : absoluteTick - currentTick;

    /// <summary>
    /// Convert an absolute next-tick value to an offset.
    /// Clamped to 0 so we never store a negative "next tick".
    /// </summary>
    public static long ToNextTickOffset(long absoluteTick, long currentTick)
        => Math.Max(0L, absoluteTick - currentTick);

    /// <summary>
    /// Convert an absolute cooldown expiration tick to remaining ticks.
    /// Returns null if the value is 0 or negative (cooldown already expired).
    /// </summary>
    public static long? ToCooldownRemaining(long absoluteTick, long currentTick)
    {
        var remaining = absoluteTick - currentTick;
        return remaining > 0 ? remaining : null;
    }

    // ── Load helpers ─────────────────────────────────────────

    /// <summary>
    /// Restore an absolute expiration tick from a saved offset.
    /// Returns Permanent if the offset is Permanent.
    /// </summary>
    public static long FromExpirationOffset(long offset, long currentTick)
        => offset == Permanent ? Permanent : currentTick + offset;

    /// <summary>
    /// Restore an absolute next-tick from a saved offset.
    /// Clamped so the tick fires no earlier than the current tick.
    /// </summary>
    public static long FromNextTickOffset(long offset, long currentTick)
        => currentTick + Math.Max(0L, offset);

    /// <summary>
    /// Restore an absolute cooldown expiration tick from saved remaining ticks.
    /// Returns 0 (expired) if remaining is null.
    /// </summary>
    public static long FromCooldownRemaining(long? remaining, long currentTick)
        => remaining.HasValue ? currentTick + remaining.Value : 0L;
}
