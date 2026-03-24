using Arch.Core;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Commands.Parser;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Core.Logging;
using MysteryMud.Domain;

namespace MysteryMud.Application.Commands.Dispatcher;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly ICommandRegistry _commandRegistry;

    public CommandDispatcher(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    public void Dispatch(SystemContext systemContext, GameState gameState, Entity actor, ReadOnlySpan<char> input)
    {
        systemContext.Log.LogDebug(LogEvents.System, "*** [{name}] EXECUTING [{input}]", actor.DebugName, input.ToString());

        // extract command and arguments
        CommandParser.SplitCommand(input, out var cmdSpan, out var argsSpan);

        // search command in registry
        if (!_commandRegistry.TryGetCommand(cmdSpan, out var cmd))
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
