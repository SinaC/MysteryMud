using Arch.Core;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class SocialsCommand : ICommand
{
    private const string Name = "socials";
    private readonly ICommandRegistry _commandRegistry;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeCommandId(),
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

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        var socialCommandDefinitions = _commandRegistry.GetCommandDefinitions<SocialCommand>();
        foreach (var chunk in socialCommandDefinitions.OrderBy(x => x.Name).Chunk(4))
        {
            systemContext.Msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x.Name,-14}")));
        }
    }
}
