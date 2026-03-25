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
        systemContext.Log.LogInformation(LogEvents.System, "*** [{name}] EXECUTING [{input}]", actor.DebugName, input.ToString());

        // extract command and arguments
        _commandParser.SplitCommand(input, out var cmdSpan, out var argsSpan);

        // search command in registry
        var findResult = _commandRegistry.Find(GameData.Enums.CommandLevel.Immortal, GameData.Enums.Position.Standing, cmdSpan); // TODO: command level and position should be determined based on actor's state, not hardcoded
        switch(findResult.Type)
        {
            case CommandFindResultType.NotFound:
                systemContext.MessageBus.Publish(actor, "Unknown command.");
                return;
            case CommandFindResultType.Ambiguous:
                systemContext.MessageBus.Publish(actor, "Ambiguous command."); // TODO
                return;
            case CommandFindResultType.NoPermission:
                systemContext.MessageBus.Publish(actor, "Permission denied."); // TODO
                return;
            case CommandFindResultType.WrongPosition:
                systemContext.MessageBus.Publish(actor, "Invalid position."); // TODO
                return; 
        }
        var command = findResult.Command!;

        // parse arguments using command-specific rules
        _commandParser.Parse(cmdSpan, argsSpan, command!.ParseOptions.ArgumentCount, command!.ParseOptions.LastIsText, out var ctx);

        // execute command
        command.Execute(systemContext, gameState, actor, ctx);
    }
}
