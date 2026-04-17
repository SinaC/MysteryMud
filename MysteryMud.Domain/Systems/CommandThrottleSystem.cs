using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class CommandThrottleSystem
{
    private const long SPAM_WINDOW = 10_000;  // 10s window for identical commands
    private const long RESET_WINDOW = 30_000; // reset violations after 30s
    private const int MAX_IDENTICAL = 2;      // max identical commands per SPAM_WINDOW
    private const int MAX_VIOLATIONS = 3;     // max spam violations before blocking

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
        state.World.Query(query, (Entity player, ref CommandBuffer buffer, ref CommandThrottle throttle, ref PlayerTag _, ref HasCommandTag _) =>
            {
                // --- refill all category buckets ---
                Refill(ref throttle.Movement, now);
                Refill(ref throttle.Combat, now);
                Refill(ref throttle.Social, now);
                Refill(ref throttle.Utility, now);
                Refill(ref throttle.Admin, now);

                // --- reset violations if enough time passed ---
                if (now - throttle.LastViolationTime > RESET_WINDOW)
                {
                    throttle.Violations = 0;
                }

                // --- prune history for SPAM_WINDOW ---
                throttle.PruneHistory(now, SPAM_WINDOW);

                bool notified = false;

                for (int i = 0; i < buffer.Count; i++)
                {
                    ref var request = ref buffer.Items[i];

                    if (request.Cancelled || request.Force)
                        continue;

                    var def = request.Command.Definition;
                    var primaryCat = GetPrimaryCategory(def.ThrottlingCategories);
                    ref var bucket = ref GetBucket(ref throttle, primaryCat);

                    float cost = GetCommandCost(ref def);

                    // --- check global WAIT_STATE ---
                    if (now < throttle.NextAllowedTime)
                    {
                        request.Cancelled = true;
                        if (!notified)
                        {
                            _msg.To(player).Send("You must wait before acting again.");
                            notified = true;
                        }
                        continue;
                    }

                    // --- check token bucket ---
                    if (bucket.Tokens < cost)
                    {
                        request.Cancelled = true;
                        if (!notified)
                        {
                            _msg.To(player).Send("You are acting too fast.");
                            notified = true;
                        }
                        continue;
                    }

/* SPAM removed
                    // --- spam detection ---
                    int identical = throttle.CountIdentical(request.CommandId);
                    if (identical >= MAX_IDENTICAL)
                    {
                        throttle.Violations++;
                        throttle.LastViolationTime = now;
                        request.Cancelled = true;
                        if (!notified)
                        { 
                            _msg.To(player).Send(GetSpamMessage(throttle.Violations));
                            notified = true;
                        }
                        continue;
                    }
*/
                    if (throttle.Violations >= MAX_VIOLATIONS)
                    {
                        request.Cancelled = true;
                        if (!notified)
                        { 
                            _msg.To(player).Send("You are sending commands too fast.");
                            notified = true;
                        }
                        continue;
                    }

                    // --- accept command ---
                    request.ExecuteAt = now;

                    // consume bucket tokens
                    bucket.Tokens -= cost;

                    // add to history
                    throttle.AddHistory(request.CommandId, now);

                    // optional WAIT_STATE lag (category-based)
                    throttle.NextAllowedTime = now + GetCommandLag(ref def);
                }
            });
    }

    // --------------------
    // Helper functions
    // --------------------

    private static void Refill(ref CommandCategoryBucket b, long now)
    {
        if (b.LastRefillTime == 0)
        {
            b.LastRefillTime = now;
            b.Tokens = b.MaxTokens;
            return;
        }

        float delta = (now - b.LastRefillTime) / 1000f; // seconds
        b.LastRefillTime = now;
        b.Tokens = Math.Min(b.MaxTokens, b.Tokens + delta * b.RefillRate);
    }

    private static string GetSpamMessage(int violations) =>
        violations switch
        {
            1 => "Slow down.",
            2 => "You are spamming commands.",
            _ => "Command input throttled."
        };

    private static CommandThrottlingCategories GetPrimaryCategory(CommandThrottlingCategories cat)
    {
        if ((cat & CommandThrottlingCategories.Movement) != 0) return CommandThrottlingCategories.Movement;
        if ((cat & CommandThrottlingCategories.Combat) != 0) return CommandThrottlingCategories.Combat;
        if ((cat & CommandThrottlingCategories.Social) != 0) return CommandThrottlingCategories.Social;
        if ((cat & CommandThrottlingCategories.Utility) != 0) return CommandThrottlingCategories.Utility;
        if ((cat & CommandThrottlingCategories.Admin) != 0) return CommandThrottlingCategories.Admin;
        return CommandThrottlingCategories.Utility;
    }

    private static ref CommandCategoryBucket GetBucket(ref CommandThrottle t, CommandThrottlingCategories cat)
    {
        switch (cat)
        {
            case CommandThrottlingCategories.Movement: return ref t.Movement;
            case CommandThrottlingCategories.Combat: return ref t.Combat;
            case CommandThrottlingCategories.Social: return ref t.Social;
            case CommandThrottlingCategories.Utility: return ref t.Utility;
            case CommandThrottlingCategories.Admin: return ref t.Admin;
            default: return ref t.Utility;
        }
    }

    private static float GetCommandCost(ref CommandDefinition def)
    {
        // Could vary per command or category
        return 1f;
    }

    private static long GetCommandLag(ref CommandDefinition def)
    {
        // Optional: category-based lag
        if ((def.ThrottlingCategories & CommandThrottlingCategories.Combat) != 0) return 300; // combat slower
        return 0; // other categories instant
    }
}