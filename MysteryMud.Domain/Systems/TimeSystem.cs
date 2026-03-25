namespace MysteryMud.Domain.Systems;

public static class TimeSystem
{
    public static long CurrentTick { get; private set; }

    public static void NextTick()
    {
        CurrentTick++;
    }
}
