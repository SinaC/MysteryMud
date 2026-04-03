using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class CommandThrottleSystem
{
    private readonly IGameMessageService _msg;

    public CommandThrottleSystem(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state)
    {
        long now = state.CurrentTimeMs;

        var query = new QueryDescription()
            .WithAll<CommandBuffer, CommandThrottle, PlayerTag, HasCommandTag>();

        state.World.Query(query,
            (Entity player, ref CommandBuffer buffer, ref CommandThrottle throttle, ref PlayerTag _, ref HasCommandTag _) =>
            {
                //Console.WriteLine($"Tokens={throttle.Tokens:0.00}, NextAllowed={throttle.NextAllowedTime}, Now={now}");

                // refill all buckets
                Refill(ref throttle.Movement, now);
                Refill(ref throttle.Combat, now);
                Refill(ref throttle.Social, now);
                Refill(ref throttle.Utility, now);
                Refill(ref throttle.Admin, now);

                bool notified = false;

                for (int i = 0; i < buffer.Count; i++)
                {
                    ref var req = ref buffer.Items[i];

                    if (req.Cancelled || req.Force)
                        continue;

                    var def = req.Command.Definition;

                    var category = GetPrimaryCategory(def.ThrottlingCategories);
                    ref var bucket = ref GetBucket(ref throttle, category);

                    float cost = GetCost(ref def);

                    // token check
                    if (bucket.Tokens < cost)
                    {
                        req.Cancelled = true;

                        if (!notified)
                        {
                            _msg.To(player).Send("You're acting too fast.");
                            notified = true;
                        }

                        continue;
                    }

                    bucket.Tokens -= cost;

                    // lag (optional: only for some categories)
                    long lag = GetLag(ref def);

                    if (lag > 0)
                        throttle.NextAllowedTime = now + lag;

                    req.ExecuteAt = now;
                }
            });
    }

    private static ref CommandCategoryBucket GetBucket(ref CommandThrottle t, CommandThrottlingCategories cat)
    {
        switch (cat)
        {
            case CommandThrottlingCategories.Movement:
                return ref t.Movement;
            case CommandThrottlingCategories.Combat:
                return ref t.Combat;
            case CommandThrottlingCategories.Social:
                return ref t.Social;
            case CommandThrottlingCategories.Utility:
                return ref t.Utility;
            case CommandThrottlingCategories.Admin:
                return ref t.Admin;
            default:
                return ref t.Utility;
        }
    }

    private static CommandThrottlingCategories GetPrimaryCategory(CommandThrottlingCategories cat)
    {
        if ((cat & CommandThrottlingCategories.Movement) != 0) return CommandThrottlingCategories.Movement;
        if ((cat & CommandThrottlingCategories.Combat) != 0) return CommandThrottlingCategories.Combat;
        if ((cat & CommandThrottlingCategories.Social) != 0) return CommandThrottlingCategories.Social;
        if ((cat & CommandThrottlingCategories.Utility) != 0) return CommandThrottlingCategories.Utility;
        if ((cat & CommandThrottlingCategories.Admin) != 0) return CommandThrottlingCategories.Admin;

        return CommandThrottlingCategories.Utility;
    }

    private static void Refill(ref CommandCategoryBucket b, long now)
    {
        if (b.LastRefillTime == 0)
        {
            b.LastRefillTime = now;
            b.Tokens = b.MaxTokens;
            return;
        }

        float delta = (now - b.LastRefillTime) / 1000f;
        b.LastRefillTime = now;

        b.Tokens = Math.Min(b.MaxTokens, b.Tokens + delta * b.RefillRate);
    }

    private static float GetCost(ref CommandDefinition def) => 1f;

    private static long GetLag(ref CommandDefinition def) => 300;
}
// complex throttling
//public class CommandThrottleSystem
//{
//    private const long SPAM_WINDOW = 10_000;
//    private const long RESET_WINDOW = 30_000;

//    private const int MAX_IDENTICAL = 2;
//    private const int MAX_VIOLATIONS = 3;

//    private readonly IGameMessageService _msg;

//    public CommandThrottleSystem(IGameMessageService msg)
//    {
//        _msg = msg;
//    }

//    public void Execute(GameState state)
//    {
//        long now = state.CurrentTimeMs;

//        var query = new QueryDescription()
//           .WithAll<CommandBuffer, CommandThrottle, PlayerTag, HasCommandTag>();
//        state.World.Query(query, (Entity player, ref CommandBuffer buffer, ref CommandThrottle throttle, ref PlayerTag _, ref HasCommandTag _) =>
//        {
//            EnsureBuffers(ref throttle);

//            // ---- reset violations over time ----
//            if (now - throttle.LastViolationTime > RESET_WINDOW)
//            {
//                throttle.Violations = 0;
//            }

//            // ---- prune history ----
//            PruneHistory(ref throttle, now);

//            for (int i = 0; i < buffer.Count; i++)
//            {
//                ref var request = ref buffer.Items[i];

//                if (request.Cancelled)
//                    continue;

//                if (request.Force)
//                    continue; // skip throttle checks

//                // =====================================
//                // 1. WAIT STATE (global delay)
//                // =====================================
//                if (now < throttle.NextAllowedTime)
//                {
//                    request.Cancelled = true;
//                    request.ExecuteAt = throttle.NextAllowedTime;
//                    //Notify(player, "You must wait before acting again.");
//                    continue;
//                }

//                // =====================================
//                // 2. COOLDOWN (per command)
//                // =====================================
//                if (IsOnCooldown(ref throttle, request.CommandId, now))
//                {
//                    request.Cancelled = true;
//                    Notify(player, "That command is not ready yet.");
//                    continue;
//                }

//                // =====================================
//                // 3. SPAM DETECTION
//                // =====================================
//                int identical = CountIdentical(ref throttle, request.CommandId);

//                if (identical >= MAX_IDENTICAL)
//                {
//                    throttle.Violations++;
//                    throttle.LastViolationTime = now;

//                    request.Cancelled = true;

//                    Notify(player, GetSpamMessage(throttle.Violations));
//                    continue;
//                }

//                if (throttle.Violations >= MAX_VIOLATIONS)
//                {
//                    request.Cancelled = true;

//                    Notify(player, "You are sending commands too fast.");
//                    continue;
//                }

//                // =====================================
//                // 4. ACCEPT → APPLY EFFECTS
//                // =====================================

//                AddHistory(ref throttle, request.CommandId, now);

//                // Apply WAIT_STATE (example: 500ms global delay)
//                throttle.NextAllowedTime = now + GetGlobalDelay(request.CommandId);

//                // Apply cooldown if needed
//                ApplyCooldown(ref throttle, request.CommandId, now);
//            }
//        });
//    }

//    // =========================
//    // Helpers
//    // =========================

//    private static void EnsureBuffers(ref CommandThrottle t)
//    {
//        t.History ??= new CommandHistoryEntry[10];
//        t.Cooldowns ??= new CooldownEntry[5];
//    }

//    private static void PruneHistory(ref CommandThrottle t, long now)
//    {
//        int write = 0;

//        for (int i = 0; i < t.HistoryCount; i++)
//        {
//            if (now - t.History[i].Timestamp <= SPAM_WINDOW)
//            {
//                t.History[write++] = t.History[i];
//            }
//        }

//        t.HistoryCount = write;
//    }

//    private static int CountIdentical(ref CommandThrottle t, int commandId)
//    {
//        int count = 0;

//        for (int i = 0; i < t.HistoryCount; i++)
//        {
//            if (t.History[i].CommandId == commandId)
//                count++;
//        }

//        return count;
//    }

//    private static void AddHistory(ref CommandThrottle t, int commandId, long now)
//    {
//        if (t.HistoryCount == t.History.Length)
//            Array.Resize(ref t.History, t.History.Length * 2);

//        t.History[t.HistoryCount++] = new CommandHistoryEntry
//        {
//            CommandId = commandId,
//            Timestamp = now
//        };
//    }

//    private static bool IsOnCooldown(ref CommandThrottle t, int commandId, long now)
//    {
//        for (int i = 0; i < t.CooldownCount; i++)
//        {
//            if (t.Cooldowns[i].CommandId == commandId)
//            {
//                return now < t.Cooldowns[i].ReadyAt;
//            }
//        }

//        return false;
//    }

//    private static void ApplyCooldown(ref CommandThrottle t, int commandId, long now)
//    {
//        long delay = GetCooldown(commandId);
//        if (delay <= 0)
//            return;

//        for (int i = 0; i < t.CooldownCount; i++)
//        {
//            if (t.Cooldowns[i].CommandId == commandId)
//            {
//                t.Cooldowns[i].ReadyAt = now + delay;
//                return;
//            }
//        }

//        if (t.CooldownCount == t.Cooldowns.Length)
//            Array.Resize(ref t.Cooldowns, t.Cooldowns.Length * 2);

//        t.Cooldowns[t.CooldownCount++] = new CooldownEntry
//        {
//            CommandId = commandId,
//            ReadyAt = now + delay
//        };
//    }

//    // =========================
//    // Config (you can externalize later)
//    // =========================

//    private static long GetGlobalDelay(int commandId) // TODO: use command category instead of commandId ?
//    {
//        //return commandId switch
//        //{
//        //    // e.g. attack → slower
//        //    1 => 1000,
//        //    _ => 300
//        //};
//        return 300;
//    }

//    private static long GetCooldown(int commandId)
//    {
//        //return commandId switch
//        //{
//        //    // e.g. "cast spell"
//        //    2 => 5000,
//        //    _ => 0
//        //};
//        return 0;
//    }

//    private static string GetSpamMessage(int violations)
//    {
//        return violations switch
//        {
//            1 => "Slow down.",
//            2 => "You are spamming commands.",
//            _ => "Command input throttled."
//        };
//    }

//    private void Notify(Entity entity, string msg)
//    {
//        _msg.To(entity).Send(msg);
//    }
//}