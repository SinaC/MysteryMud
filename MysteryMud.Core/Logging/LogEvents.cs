using Microsoft.Extensions.Logging;

namespace MysteryMud.Core.Logging;

public static class LogEvents
{
    public static readonly EventId System = new(1, "System");
    public static readonly EventId Dot = new(2, "Dot");
    public static readonly EventId Hot = new(3, "Hot");
    public static readonly EventId Damage = new(4, "Damage");
    public static readonly EventId Heal = new(5, "Heal");
    public static readonly EventId Factory = new(6, "Factory");
    public static readonly EventId Cleanup = new(7, "Cleanup");
    public static readonly EventId Duration = new(8, "Duration");
    public static readonly EventId Respawn = new(9, "Respawn");
}