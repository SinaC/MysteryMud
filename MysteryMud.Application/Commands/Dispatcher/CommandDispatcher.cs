using Arch.Core;
using Microsoft.Extensions.Logging;
using MysteryMud.Domain;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Application.Commands.Registry;
using MysteryMud.Application.Commands.Parser;

namespace MysteryMud.Application.Commands.Dispatcher;

public static class CommandDispatcher
{
    public static void Dispatch(SystemContext systemContext, GameState gameState, Entity actor, ReadOnlySpan<char> input)
    {
        systemContext.Log.LogDebug(LogEvents.System, "*** [{name}] EXECUTING [{input}]", actor.DebugName, input.ToString());

        // extract command and arguments
        CommandParser.SplitCommand(input, out var cmdSpan, out var argsSpan);

        // search command in registry
        if (!CommandRegistry.TryGet(cmdSpan, out var cmd))
        {
            systemContext.MessageBus.Publish(actor, "Unknown command.");
            return;
        }

        // parse arguments using command-specific rules
        CommandParser.Parse(cmd!.ParseMode, cmdSpan, argsSpan, out var ctx);

        // execute command
        cmd.Execute(systemContext, gameState, actor, ctx);
    }
}
