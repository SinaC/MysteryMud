using MysteryMud.Domain.Commands;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct CommandThrottle
{
    // Per-category token buckets
    public CommandCategoryBucket Movement;
    public CommandCategoryBucket Combat;
    public CommandCategoryBucket Social;
    public CommandCategoryBucket Utility;
    public CommandCategoryBucket Admin;

    // Spam tracking
    private CommandHistoryEntry[] _history;
    public int HistoryCount { get; private set; }

    public int Violations;
    public long LastViolationTime;

    public long NextAllowedTime;

    // ===========================
    // History helpers
    // ===========================

    public void AddHistory(int commandId, long timestamp)
    {
        EnsureHistoryCapacity();

        _history[HistoryCount++] = new CommandHistoryEntry
        {
            CommandId = commandId,
            Timestamp = timestamp
        };
    }

    public void PruneHistory(long now, long spamWindow)
    {
        if (_history == null) return;

        int write = 0;
        for (int i = 0; i < HistoryCount; i++)
        {
            if (now - _history[i].Timestamp <= spamWindow)
            {
                _history[write++] = _history[i];
            }
        }
        HistoryCount = write;
    }

    public int CountIdentical(int commandId)
    {
        int count = 0;
        if (_history == null) return 0;

        for (int i = 0; i < HistoryCount; i++)
        {
            if (_history[i].CommandId == commandId)
                count++;
        }
        return count;
    }

    private void EnsureHistoryCapacity()
    {
        if (_history == null)
            _history = new CommandHistoryEntry[10];
        else if (HistoryCount == _history.Length)
            Array.Resize(ref _history, _history.Length * 2);
    }
}

//public class CommandThrottle // this MUST be a class (see CommandBuffer)
//{
//    // one by CommandCategories entry
//    public CommandCategoryBucket Movement;
//    public CommandCategoryBucket Combat;
//    public CommandCategoryBucket Social;
//    public CommandCategoryBucket Utility;
//    public CommandCategoryBucket Admin;

//    public long NextAllowedTime; // global lag (WAIT_STATE)

//    // complex throttling
//    //// ---- spam tracking ----
//    public CommandHistoryEntry[] History = new CommandHistoryEntry[10]; // last N executed commands
//    public int HistoryCount;

//    public int Violations;
//    public long LastViolationTime;

//    //// ---- per-command cooldowns ----
//    //public CooldownEntry[] Cooldowns;
//    //public int CooldownCount;
//}
