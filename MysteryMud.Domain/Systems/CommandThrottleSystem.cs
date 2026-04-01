namespace MysteryMud.Domain.Systems;

/*
public class CommandThrottleSystem
{
    private const long SPAM_WINDOW = 10_000;
    private const long RESET_WINDOW = 30_000;

    private const int MAX_IDENTICAL = 2;
    private const int MAX_VIOLATIONS = 3;

    public void Execute(GameState state)
    {
        long now = state.CurrentTimeMs;

        var query = new QueryDescription()
           .WithAll<CommandBuffer, CommandHistory, Violation, PlayerTag, HasCommandTag>();
        state.World.Query(query, (Entity player, ref CommandBuffer buffer, ref CommandHistory history, ref Violation violations, ref PlayerTag _, ref HasCommandTag _) =>
        {
            ref var throttle = ref player.Get<CommandThrottleComponent>();

            EnsureBuffers(ref throttle);

            // ---- reset violations over time ----
            if (now - throttle.LastViolationTime > RESET_WINDOW)
            {
                throttle.Violations = 0;
            }

            // ---- prune history ----
            PruneHistory(ref throttle, now);

            for (int i = 0; i < buffer.Count; i++)
            {
                ref var request = ref buffer.Items[i];

                if (request.Cancelled)
                    continue;

                // =====================================
                // 1. WAIT STATE (global delay)
                // =====================================
                if (now < throttle.NextAllowedTime)
                {
                    request.Cancelled = true;
                    Notify(entity, "You must wait before acting again.");
                    continue;
                }

                // =====================================
                // 2. COOLDOWN (per command)
                // =====================================
                if (IsOnCooldown(ref throttle, request.CommandId, now))
                {
                    request.Cancelled = true;
                    Notify(entity, "That command is not ready yet.");
                    continue;
                }

                // =====================================
                // 3. SPAM DETECTION
                // =====================================
                int identical = CountIdentical(ref throttle, request.CommandId);

                if (identical >= MAX_IDENTICAL)
                {
                    throttle.Violations++;
                    throttle.LastViolationTime = now;

                    request.Cancelled = true;

                    Notify(entity, GetSpamMessage(throttle.Violations));
                    continue;
                }

                if (throttle.Violations >= MAX_VIOLATIONS)
                {
                    request.Cancelled = true;

                    Notify(entity, "You are sending commands too fast.");
                    continue;
                }

                // =====================================
                // 4. ACCEPT → APPLY EFFECTS
                // =====================================

                AddHistory(ref throttle, request.CommandId, now);

                // Apply WAIT_STATE (example: 500ms global delay)
                throttle.NextAllowedTime = now + GetGlobalDelay(request.CommandId);

                // Apply cooldown if needed
                ApplyCooldown(ref throttle, request.CommandId, now);
            }
        }
    }

    // =========================
    // Helpers
    // =========================

    private static void EnsureBuffers(ref CommandThrottleComponent t)
    {
        if (t.History == null)
            t.History = new CommandHistoryEntry[10];

        if (t.Cooldowns == null)
            t.Cooldowns = new CooldownEntry[5];
    }

    private static void PruneHistory(ref CommandThrottleComponent t, long now)
    {
        int write = 0;

        for (int i = 0; i < t.HistoryCount; i++)
        {
            if (now - t.History[i].Timestamp <= SPAM_WINDOW)
            {
                t.History[write++] = t.History[i];
            }
        }

        t.HistoryCount = write;
    }

    private static int CountIdentical(ref CommandThrottleComponent t, int commandId)
    {
        int count = 0;

        for (int i = 0; i < t.HistoryCount; i++)
        {
            if (t.History[i].CommandId == commandId)
                count++;
        }

        return count;
    }

    private static void AddHistory(ref CommandThrottleComponent t, int commandId, long now)
    {
        if (t.HistoryCount == t.History.Length)
            Array.Resize(ref t.History, t.History.Length * 2);

        t.History[t.HistoryCount++] = new CommandHistoryEntry
        {
            CommandId = commandId,
            Timestamp = now
        };
    }

    private static bool IsOnCooldown(ref CommandThrottleComponent t, int commandId, long now)
    {
        for (int i = 0; i < t.CooldownCount; i++)
        {
            if (t.Cooldowns[i].CommandId == commandId)
            {
                return now < t.Cooldowns[i].ReadyAt;
            }
        }

        return false;
    }

    private static void ApplyCooldown(ref CommandThrottleComponent t, int commandId, long now)
    {
        long delay = GetCooldown(commandId);
        if (delay <= 0)
            return;

        for (int i = 0; i < t.CooldownCount; i++)
        {
            if (t.Cooldowns[i].CommandId == commandId)
            {
                t.Cooldowns[i].ReadyAt = now + delay;
                return;
            }
        }

        if (t.CooldownCount == t.Cooldowns.Length)
            Array.Resize(ref t.Cooldowns, t.Cooldowns.Length * 2);

        t.Cooldowns[t.CooldownCount++] = new CooldownEntry
        {
            CommandId = commandId,
            ReadyAt = now + delay
        };
    }

    // =========================
    // Config (you can externalize later)
    // =========================

    private static long GetGlobalDelay(int commandId)
    {
        return commandId switch
        {
            // e.g. attack → slower
            1 => 1000,
            _ => 300
        };
    }

    private static long GetCooldown(int commandId)
    {
        return commandId switch
        {
            // e.g. "cast spell"
            2 => 5000,
            _ => 0
        };
    }

    private static string GetSpamMessage(int violations)
    {
        return violations switch
        {
            1 => "Slow down.",
            2 => "You are spamming commands.",
            _ => "Command input throttled."
        };
    }

    private static void Notify(Entity entity, string msg)
    {
        // hook messaging system
    }
}
*/