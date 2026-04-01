using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Commands;
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

        // search command in registry
        var findResult = _commandRegistry.Find(CommandLevelKind.Immortal, PositionKind.Standing, cmdSpan, out var command); // TODO: command level and position should be determined based on actor's state, not hardcoded
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
        buffer.Items ??= new CommandRequest[16]; // small initial capacity

        if (buffer.Count >= MAX_COMMANDS_PER_TICK)
        {
            // TODO: message ?
            // drop or ignore new commands
        }
        else if (buffer.Count < buffer.Items.Length) // add command to command buffer
        {
            buffer.Items[buffer.Count++] = new CommandRequest
            {
                Command = command!, // TODO: commandId

                RawCommand = cmdSpan.ToString(),
                RawArgs = argsSpan.ToString(),

                Cancelled = false
            };
            if (!actor.Has<HasCommandTag>())
                actor.Add<HasCommandTag>();
        }
        else
        {
            // TODO
            // optional: drop, overwrite, or expand
            //Array.Resize(ref commandBuffer.Items, commandBuffer.Items.Length * 2);
        }
    }
}
