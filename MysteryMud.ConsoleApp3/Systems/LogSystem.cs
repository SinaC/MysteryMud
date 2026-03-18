namespace MysteryMud.ConsoleApp3.Systems;

public static class LogSystem
{
    public static void Log(string message)
    {
        Console.WriteLine($"[{TimeSystem.CurrentTick:D5}] " + message);
    }
}
