using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Dispatching;

public class CommandDispatcher : ICommandDispatcher
{
    private const int MAX_COMMANDS_PER_TICK = 10;

    private readonly ICommandRegistry _commandRegistry;
    private readonly IGameMessageService _msg;

    public CommandDispatcher(ICommandRegistry commandRegistry, IGameMessageService msg)
    {
        _commandRegistry = commandRegistry;
        _msg = msg;
    }

    public void Dispatch(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> input)
    {
        if (!actor.IsAlive())
            return;

        // Get or create buffer
        ref var buffer = ref actor.Get<CommandBuffer>();

        if (buffer.Count >= MAX_COMMANDS_PER_TICK)
            return;

        var inputStr = input.ToString(); // ONE allocation

        // extract command and arguments
        CommandParser.SplitCommand(input, out var cmdStart, out var cmdLength, out var argsStart, out var argsLength);
        var cmdSpan = input.Slice(cmdStart, cmdLength);

        // get command level
        ref var commandLevel = ref actor.Get<CommandLevel>();

        // search command in registry
        var findResult = _commandRegistry.Find(commandLevel.Value, PositionKind.Standing, cmdSpan, out var registeredCommand); // TODO: position should be determined based on actor's state, not hardcoded
        switch (findResult)
        {
            case CommandFindResult.NotFound:
                _msg.To(actor).Send("Unknown command.");
                return;
            case CommandFindResult.NoPermission:
                _msg.To(actor).Send("Permission denied."); // TODO
                return;
            case CommandFindResult.WrongPosition:
                _msg.To(actor).Send("Invalid position."); // TODO
                return;
        }

        // add command request to command buffer
        buffer.Add(new CommandRequest
        {
            Command = registeredCommand!,
            CommandId = registeredCommand!.Definition.Id,

            Input = inputStr,
            CmdStart = cmdStart,
            CmdLength = cmdLength,
            ArgsStart = argsStart,
            ArgsLength = argsLength,

            Cancelled = false,
            Force = false
        });
        if (!actor.Has<HasCommandTag>())
            actor.Add<HasCommandTag>();
    }
}
