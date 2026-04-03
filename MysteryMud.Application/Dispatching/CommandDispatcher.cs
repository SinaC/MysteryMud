using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Dispatching;

public class CommandDispatcher : ICommandDispatcher
{
    private const int MAX_COMMANDS_PER_TICK = 10;

    private readonly ICommandRegistry _commandRegistry;

    public CommandDispatcher(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    public void Dispatch(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> input)
    {
        systemContext.Log.LogInformation(LogEvents.System, "*** [{name}] DISPATCHING [{input}]", actor.DebugName, input.ToString());

        // extract command and arguments
        CommandParser.SplitCommand(input, out var cmdSpan, out var argsSpan);

        // get command level
        ref var commandLevel = ref actor.Get<CommandLevel>();

        // search command in registry
        var findResult = _commandRegistry.Find(commandLevel.Value, PositionKind.Standing, cmdSpan, out var command); // TODO: position should be determined based on actor's state, not hardcoded
        switch (findResult)
        {
            case CommandFindResult.NotFound:
                systemContext.Msg.To(actor).Send("Unknown command.");
                return;
            case CommandFindResult.NoPermission:
                systemContext.Msg.To(actor).Send("Permission denied."); // TODO
                return;
            case CommandFindResult.WrongPosition:
                systemContext.Msg.To(actor).Send("Invalid position."); // TODO
                return;
        }

        // get command buffer
        ref var buffer = ref actor.Get<CommandBuffer>();

        // create items array if needed
        buffer.Items ??= new CommandRequest[8]; // small initial capacity

        if (buffer.Count >= MAX_COMMANDS_PER_TICK)
        {
            // TODO: message ?
            // drop or ignore new commands
        }
        else
        {
            if (buffer.Count == buffer.Items.Length)
                Array.Resize(ref buffer.Items, buffer.Items.Length * 2); // expand if needed
            buffer.Items[buffer.Count++] = new CommandRequest
            {
                Command = command!,
                CommandId = command!.Definition.Id,

                RawCommand = cmdSpan.ToString(),
                RawArgs = argsSpan.ToString(),

                Cancelled = false,
                Force = false
            };
            if (!actor.Has<HasCommandTag>())
                actor.Add<HasCommandTag>();
        }
    }
}
