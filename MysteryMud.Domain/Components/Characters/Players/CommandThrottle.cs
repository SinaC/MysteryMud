using MysteryMud.Domain.Commands;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct CommandThrottle
{
    // ---- spam tracking ----
    public CommandHistoryEntry[] History;
    public int HistoryCount;

    public int Violations;
    public long LastViolationTime;

    // ---- wait state (global delay) ----
    public long NextAllowedTime;

    // ---- per-command cooldowns ----
    public CooldownEntry[] Cooldowns;
    public int CooldownCount;
}
