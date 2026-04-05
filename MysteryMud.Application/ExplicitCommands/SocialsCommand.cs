using Arch.Core;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class SocialsCommand : IExplicitCommand
{
    private const string Name = "socials";
    private readonly ICommandRegistry _commandRegistry;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeUniqueId(),
        Name = Name,
        Aliases = [],
        CannotBeForced = false,
        RequiredLevel = CommandLevelKind.Player,
        MinimumPosition = PositionKind.Dead,
        Priority = 0,
        AllowAbbreviation = true,
        HelpText = "Display list of available socials.",
        Syntaxes = ["[cmd]"],
        Categories = ["information"],
        ThrottlingCategories = CommandThrottlingCategories.Utility,
    };

    public SocialsCommand(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        var socialCommandDefinitions = _commandRegistry.GetCommands<SocialCommand>();
        foreach (var chunk in socialCommandDefinitions.OrderBy(x => x.Definition.Name).Chunk(4))
        {
            executionContext.Msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x.Definition.Name,-14}")));
        }
    }
}
