namespace MysteryMud.GameData.Time;

public static class TimeConversion
{
    public const decimal SecondsPerTick = 1.0m / TimeRate.TicksPerSecond;

    // JSON / ability definitions → internal ticks
    public static long SecondsToTicks(decimal seconds) =>
        (long)Math.Round(seconds * TimeRate.TicksPerSecond);

    // Ticks → display seconds (for player-facing UI)
    public static decimal TicksToSeconds(long ticks) =>
        ticks * SecondsPerTick;

    // Ticks → human-readable string (e.g. "1m 23s" or "45s")
    public static string TicksToDisplay(long ticks)
    {
        var totalSeconds = (int)Math.Floor(ticks * SecondsPerTick);
        var tenths = (int)(ticks % TimeRate.TicksPerSecond); // remaining ticks after whole seconds

        var h = totalSeconds / 3600;
        var m = (totalSeconds % 3600) / 60;
        var s = totalSeconds % 60;

        // Only show tenths for short durations where it's meaningful
        bool showTenths = totalSeconds < 5 && tenths > 0;

        return (h, m, s, showTenths) switch
        {
            ( > 0, > 0, > 0, _) => $"{h}h {m}m {s}s",
            ( > 0, > 0, 0, _) => $"{h}h {m}m",
            ( > 0, 0, > 0, _) => $"{h}h {s}s",
            ( > 0, 0, 0, _) => $"{h}h",
            (0, > 0, > 0, _) => $"{m}m {s}s",
            (0, > 0, 0, _) => $"{m}m",
            (0, 0, > 0, true) => $"{s}.{tenths}s",
            (0, 0, > 0, false) => $"{s}s",
            (0, 0, 0, true) => $"0.{tenths}s",
            _ => "0s"
        };
    }
}
