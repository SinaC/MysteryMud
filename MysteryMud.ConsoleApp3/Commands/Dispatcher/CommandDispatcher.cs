using Arch.Core;
using Microsoft.Extensions.Logging;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Commands.Registry;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Logging;
using MysteryMud.ConsoleApp3.Components.Extensions;

namespace MysteryMud.ConsoleApp3.Commands.Dispatcher;

class CommandDispatcher
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
        CommandParser.Parse(cmd.ParseMode, cmdSpan, argsSpan, out var ctx);

        // execute command
        cmd.Execute(systemContext, gameState, actor, ctx);
    }
}
