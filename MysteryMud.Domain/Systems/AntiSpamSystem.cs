using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;

namespace MysteryMud.Domain.Systems;

// TODO: adapt for CommandThrottle if CommandThrottleSystem seems weird
//public class AntiSpamSystem
//{
//    private const int MAX_IDENTICAL = 2;
//    private const int MAX_VIOLATIONS = 3;

//    private const long WINDOW_MS = 10_000;   // 10 seconds
//    private const long RESET_MS = 30_000;    // reset violations

//    private readonly IGameMessageService _msg;

//    public AntiSpamSystem(IGameMessageService msg)
//    {
//        _msg = msg;
//    }

//    public void Execute(GameState state)
//    {
//        long now = state.CurrentTimeMs;

//        var query = new QueryDescription()
//            .WithAll<CommandBuffer, CommandHistory, Violation, PlayerTag, HasCommandTag>();
//        state.World.Query(query, (Entity player, ref CommandBuffer buffer, ref CommandHistory history, ref Violation violations, ref PlayerTag _, ref HasCommandTag _) =>
//        {
//            // ---------- ensure buffers ----------
//            if (history.Buffer == null)
//                history.Buffer = new CommandHistoryEntry[10];

//            // ---------- reset violations over time ----------
//            if (now - violations.LastViolationTick > RESET_MS)
//            {
//                violations.Count = 0;
//            }

//            // ---------- prune old history ----------
//            PruneHistory(ref history, now);

//            // ---------- process each command ----------
//            for (int i = 0; i < buffer.Count; i++)
//            {
//                ref var request = ref buffer.Items[i];

//                if (request.Cancelled)
//                    continue;

//                // ----- count identical commands in window -----
//                int identical = CountIdentical(ref history, request.CommandId);

//                if (identical >= MAX_IDENTICAL)
//                {
//                    violations.Count++;
//                    violations.LastViolationTick = now;

//                    request.Cancelled = true;

//                    Notify(player, GetThrottleMessage(violations.Count));
//                    continue;
//                }

//                // ----- global violation throttle -----
//                if (violations.Count >= MAX_VIOLATIONS)
//                {
//                    request.Cancelled = true;

//                    Notify(player, GetThrottleMessage(violations.Count));
//                    continue;
//                }

//                // ----- record command -----
//                AddHistory(ref history, request.CommandId, now);
//            }
//        });
//    }

//    // ==============================
//    // Helpers
//    // ==============================

//    private static void PruneHistory(ref CommandHistory history, long now)
//    {
//        int write = 0;

//        for (int i = 0; i < history.Count; i++)
//        {
//            if (now - history.Buffer[i].Timestamp <= WINDOW_MS)
//            {
//                history.Buffer[write++] = history.Buffer[i];
//            }
//        }

//        history.Count = write;
//    }

//    private static int CountIdentical(ref CommandHistory history, int commandId)
//    {
//        int count = 0;

//        for (int i = 0; i < history.Count; i++)
//        {
//            if (history.Buffer[i].CommandId == commandId)
//                count++;
//        }

//        return count;
//    }

//    private static void AddHistory(ref CommandHistory history, int commandId, long now)
//    {
//        if (history.Count == history.Buffer.Length)
//        {
//            Array.Resize(ref history.Buffer, history.Buffer.Length * 2);
//        }

//        history.Buffer[history.Count++] = new CommandHistoryEntry
//        {
//            CommandId = commandId,
//            Timestamp = now
//        };
//    }

//    private void Notify(Entity entity, string message)
//    {
//        _msg.To(entity).Send(message);
//    }

//    private static string GetThrottleMessage(int violations)
//    {
//        return violations switch
//        {
//            1 => "You are sending commands too quickly.",
//            2 => "Slow down.",
//            _ => "Command input temporarily throttled."
//        };
//    }
//}
