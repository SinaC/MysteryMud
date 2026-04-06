using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class ForceCommand : IExplicitCommand
{
    private const string Name = "force";
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    private readonly ILogger _logger;
    private readonly ICommandRegistry _commandRegistry;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeUniqueId(),
        Name = Name,
        Aliases = [],
        CannotBeForced = true,
        RequiredLevel = CommandLevelKind.Admin,
        MinimumPosition = PositionKind.Dead,
        Priority = 0,
        DisallowAbbreviation = true,
        HelpText = @"[cmd] forces one character to execute a command, except of course delete.

[cmd] 'all' forces all player characters to execute a command.
This is typically used for 'force all save'.",
        Syntaxes = ["[cmd] <character> <command>", "[cmd] all <command>"],
        Categories = ["punish"],
        ThrottlingCategories = CommandThrottlingCategories.Admin,
    };

    public ForceCommand(ILogger logger, ICommandRegistry commandRegistry)
    {
        _logger = logger;
        _commandRegistry = commandRegistry;
    }

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0 || ctx.Text.Length == 0)
        {
            executionContext.Msg.To(actor).Send("Force whom what?");
            return;
        }

        // TODO: force all (see TellCommand)

        // search target
        ref var people = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;  // TODO: in world
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, people);

        if (target == default)
        {
            executionContext.Msg.To(actor).Send("They aren't here.");
            return;
        }

        if (target == actor)
        {
            executionContext.Msg.To(actor).Send("They aren't here.");
            return;
        }

        // get target position
        ref var targetPosition = ref target.Get<Position>();

        var inputStr = ctx.Text.ToString(); // ONE allocation

        // split command/args
        CommandParser.SplitCommand(ctx.Text, out var forcedCmdStart, out var forcedCmdLength, out var forcedArgsStart, out var forcedArgsLength);

        // search command // TODO: command level
        var findResult = _commandRegistry.Find(CommandLevelKind.Player, targetPosition.Value, ctx.Text.Slice(forcedCmdStart, forcedCmdLength), out var forcedCommand);
        if (findResult == CommandFindResult.NotFound)
        {
            executionContext.Msg.To(actor).Send("Command not found.");
            return;
        }
        else if (findResult == CommandFindResult.WrongPosition)
        {
            executionContext.Msg.To(actor).Send($"{target.DisplayName} is in the wrong position.");
            return;
        }
        else if (findResult == CommandFindResult.NoPermission)
        {
            executionContext.Msg.To(actor).Send($"{target.DisplayName} is not allowed to use this command.");
            return;
        }
        else if (forcedCommand is null)
        {
            _logger.LogError("ForceCommand: command registry returned null command when trying to find {cmd}", ctx.Text.ToString());
            executionContext.Msg.To(actor).Send("Something goes wrong.");
            return;
        }
        else if (forcedCommand!.Definition.CannotBeForced)
        {
            executionContext.Msg.To(actor).Send("That will NOT be done.");
            return;
        }

        // add forced command request to target command buffer
        ref var buffer = ref target.Get<CommandBuffer>();
        buffer.Add(new CommandRequest
        {
            Command = forcedCommand,
            CommandId = forcedCommand.Definition.Id,

            Input = inputStr,
            CmdStart = forcedCmdStart,
            CmdLength = forcedCmdLength,
            ArgsStart = forcedArgsStart,
            ArgsLength = forcedArgsLength,

            Cancelled = false,
            Force = true
        });
        if (!target.Has<HasCommandTag>())
            target.Add<HasCommandTag>();

        // if forced commands must be executed before other
        //Array.Copy(buffer.Items, 0, buffer.Items, 1, buffer.Count);
        //buffer.Items[0] = new CommandRequest { CommandId = ..., Force = true };
        //buffer.Count++;
    }
}
