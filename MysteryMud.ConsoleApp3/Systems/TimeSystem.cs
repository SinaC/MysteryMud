namespace MysteryMud.ConsoleApp3.Systems;

public static class TimeSystem
{
    // TODO
    public static int CurrentTick { get; private set; }

    public static void NextTick()
    {
        CurrentTick++;
    }
}
