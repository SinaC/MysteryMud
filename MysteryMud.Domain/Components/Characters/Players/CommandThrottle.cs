using MysteryMud.Domain.Commands;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct CommandThrottle
{
    // one by CommandCategories entry
    public CommandCategoryBucket Movement;
    public CommandCategoryBucket Combat;
    public CommandCategoryBucket Social;
    public CommandCategoryBucket Utility;
    public CommandCategoryBucket Admin;

    public long NextAllowedTime; // global lag (WAIT_STATE)

    // complex throttling
    //// ---- spam tracking ----
    //public CommandHistoryEntry[] History;
    //public int HistoryCount;

    //public int Violations;
    //public long LastViolationTime;

    //// ---- wait state (global delay) ----
    //public long NextAllowedTime;

    //// ---- per-command cooldowns ----
    //public CooldownEntry[] Cooldowns;
    //public int CooldownCount;
}
