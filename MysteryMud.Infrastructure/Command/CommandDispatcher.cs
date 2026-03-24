using Arch.Core;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Core.Logging;
using MysteryMud.Domain;

namespace MysteryMud.Infrastructure.Command;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly ICommandParser _commandParser;

    public CommandDispatcher(ICommandRegistry commandRegistry, ICommandParser commandParser)
    {
        _commandRegistry = commandRegistry;
        _commandParser = commandParser;
    }

    public void Dispatch(SystemContext systemContext, GameState gameState, Entity actor, ReadOnlySpan<char> input)
    {
        systemContext.Log.LogDebug(LogEvents.System, "*** [{name}] EXECUTING [{input}]", actor.DebugName, input.ToString());

        // extract command and arguments
        _commandParser.SplitCommand(input, out var cmdSpan, out var argsSpan);

        // search command in registry
        if (!_commandRegistry.TryGetCommand(cmdSpan, out var cmd))
        {
            systemContext.MessageBus.Publish(actor, "Unknown command.");
            return;
        }

        // parse arguments using command-specific rules
        _commandParser.Parse(cmdSpan, argsSpan, cmd!.ParseOptions.ArgumentCount, cmd!.ParseOptions.LastIsText, out var ctx);

        // execute command
        cmd.Execute(systemContext, gameState, actor, ctx);
    }
}
