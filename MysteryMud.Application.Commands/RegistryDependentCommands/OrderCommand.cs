using Microsoft.Extensions.Logging;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.RegistryDependentCommands;

public sealed class OrderCommand : IExplicitCommand
{
    private const string Name = "order";
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    private readonly World _world;
    private readonly ILogger _logger;
    private readonly ICommandRegistry _commandRegistry;
    private readonly IGameMessageService _msg;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeUniqueId(),
        Name = Name,
        Aliases = [],
        CannotBeForced = true,
        RequiredLevel = CommandLevelKind.Player,
        MinimumPosition = PositionKind.Resting,
        Priority = 0,
        DisallowAbbreviation = false,
        HelpText = @"[cmd] orders one or all of your charmed followers (including pets) to
perform any command.  The command may have arguments.  You are responsible
for the actions of your followers, and others who attack your followers
will incur the same penalty as if they attacked you directly.

Most charmed creatures lose their aggresive nature (while charmed).

If your charmed creature engages in combat, that will break the charm.",
        Syntaxes = ["[cmd] <pet|charmie> command", "[cmd] all <command>"],
        Categories = ["group", "pet"],
        ThrottlingCategories = CommandThrottlingCategories.Utility,
    };

    public OrderCommand(World world, ILogger logger, ICommandRegistry commandRegistry, IGameMessageService msg)
    {
        _world = world;
        _logger = logger;
        _commandRegistry = commandRegistry;
        _msg = msg;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0 || ctx.Text.Length == 0)
        {
            _msg.To(actor).Send("Order whom what?");
            return;
        }

        ref var charmies = ref _world.TryGetRef<Charmies>(actor, out var hasCharmies);
        if (!hasCharmies)
        {
            _msg.To(actor).Send("You don't have followers.");
            return;
        }

        // TODO: force all (see TellCommand)

        // search target
        var target = CommandEntityFinder.SelectSingleTarget(_world, actor, ctx.Primary, charmies.Entities);
        if (target == null)
        {
            _msg.To(actor).Send("They aren't here.");
            return;
        }

        // get target position
        ref var targetPosition = ref _world.Get<Position>(target.Value);

        var inputStr = ctx.Text.ToString(); // ONE allocation

        // split command/args
        CommandParser.SplitCommand(ctx.Text, out var orderedCmdStart, out var orderedCmdLength, out var orderedArgsStart, out var orderedArgsLength);

        // search command // TODO: command level
        var findResult = _commandRegistry.Find(CommandLevelKind.Player, targetPosition.Value, ctx.Text.Slice(orderedCmdStart, orderedCmdLength), out var orderedCommand);
        if (findResult == CommandFindResult.NotFound)
        {
            _msg.To(actor).Send("Command not found.");
            return;
        }
        else if (findResult == CommandFindResult.WrongPosition)
        {
            _msg.To(actor).Act("{0} is in the wrong position.").With(target.Value);
            return;
        }
        else if (findResult == CommandFindResult.NoPermission)
        {
            _msg.To(actor).Act("{0} is not allowed to use this command.").With(target.Value);
            return;
        }
        else if (orderedCommand is null)
        {
            _logger.LogError("OrderCommand: command registry returned null command when trying to find {cmd}", ctx.Text.ToString());
            _msg.To(actor).Send("Something goes wrong.");
            return;
        }

        // add ordered command request to target command buffer
        ref var buffer = ref _world.Get<CommandBuffer>(target.Value);
        buffer.Add(new CommandRequest
        {
            Command = orderedCommand,
            CommandId = orderedCommand.Definition.Id,

            Input = inputStr,
            CmdStart = orderedCmdStart,
            CmdLength = orderedCmdLength,
            ArgsStart = orderedArgsStart,
            ArgsLength = orderedArgsLength,

            Cancelled = false,
            Order = true
        });
        if (!_world.Has<HasCommandTag>(target.Value))
            _world.Add<HasCommandTag>(target.Value);
    }
}
