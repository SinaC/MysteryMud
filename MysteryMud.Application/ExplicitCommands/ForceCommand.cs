using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Dispatching;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
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

public class ForceCommand : ICommand
{
    private const string Name = "force";
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    private readonly ICommandRegistry _commandRegistry;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeCommandId(),
        Name = Name,
        Aliases = [],
        CannotBeForced = true,
        RequiredLevel = CommandLevelKind.Admin,
        MinimumPosition = PositionKind.Dead,
        Priority = 0,
        AllowAbbreviation = true,
        HelpText = @"[cmd] forces one character to execute a command, except of course delete.

[cmd] 'all' forces all player characters to execute a command.
This is typically used for 'force all save'.",
        Syntaxes = ["[cmd] <character> <command>", "[cmd] all <command>"],
        Categories = ["punish"],
    };

    public ForceCommand(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0 || ctx.Text.Length == 0)
        {
            systemContext.Msg.To(actor).Send("Force whom what?");
            return;
        }

        // TODO: force all

        // search target
        ref var people = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;  // TODO: in world
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, people);

        if (target == default)
        {
            systemContext.Msg.To(actor).Send("They aren't here.");
            return;
        }

        if (target == actor)
        {
            systemContext.Msg.To(actor).Send("They aren't here.");
            return;
        }

        // get target position
        ref var targetPosition = ref target.Get<Position>();

        // split command/args
        CommandParser.SplitCommand(ctx.Text, out var forcedCmd, out var forcedArgs);

        // search command // TODO: command level
        var findResult = _commandRegistry.Find(CommandLevelKind.Player, targetPosition.Value, forcedCmd, out var forcedCommand);
        if (findResult == CommandFindResult.NotFound)
        {
            systemContext.Msg.To(actor).Send("Command not found.");
            return;
        }
        else if (findResult == CommandFindResult.WrongPosition)
        {
            systemContext.Msg.To(actor).Send($"{target.DisplayName} is in the wrong position.");
            return;
        }
        else if (findResult == CommandFindResult.NoPermission)
        {
            systemContext.Msg.To(actor).Send($"{target.DisplayName} is not allowed to use this command.");
            return;
        }
        else if (forcedCommand is null)
        {
            systemContext.Msg.To(actor).Send("Something goes wrong.");
            systemContext.Log.LogError("ForceCommand: command registry returned null command when trying to find {cmd}", forcedCmd.ToString());
            return;
        }
        else if (forcedCommand!.Definition.CannotBeForced)
        {
            systemContext.Msg.To(actor).Send("That will NOT be done.");
            return;
        }

        // add forced command to target command buffer
        ref var buffer = ref target.Get<CommandBuffer>();
        buffer.Items ??= new CommandRequest[16];
        if (buffer.Count == buffer.Items.Length)
            Array.Resize(ref buffer.Items, buffer.Items.Length * 2); // expand if needed
        buffer.Items[buffer.Count++] = new CommandRequest
        {
            Command = forcedCommand,
            CommandId = forcedCommand.Definition.Id,
            
            RawCommand = forcedCmd.ToString(),
            RawArgs = forcedArgs.ToString(),

            Cancelled = false,
            Force = true
        };
        if (!target.Has<HasCommandTag>())
            target.Add<HasCommandTag>();

        // if forced commands must be executed before other
        //Array.Copy(buffer.Items, 0, buffer.Items, 1, buffer.Count);
        //buffer.Items[0] = new CommandRequest { CommandId = ..., Force = true };
        //buffer.Count++;
    }
}
