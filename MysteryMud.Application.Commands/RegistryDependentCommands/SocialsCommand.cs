using DefaultEcs;
using MysteryMud.Application.Commands.DataDrivenCommands;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.RegistryDependentCommands;

public sealed class SocialsCommand : IExplicitCommand
{
    private const string Name = "socials";

    private readonly ICommandRegistry _commandRegistry;
    private readonly IGameMessageService _msg;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeUniqueId(),
        Name = Name,
        Aliases = [],
        CannotBeForced = false,
        RequiredLevel = CommandLevelKind.Player,
        MinimumPosition = PositionKind.Dead,
        Priority = 0,
        DisallowAbbreviation = false,
        HelpText = "Display list of available socials.",
        Syntaxes = ["[cmd]"],
        Categories = ["information"],
        ThrottlingCategories = CommandThrottlingCategories.Utility,
    };

    public SocialsCommand(ICommandRegistry commandRegistry, IGameMessageService msg)
    {
        _commandRegistry = commandRegistry;
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        var socialCommandDefinitions = _commandRegistry.GetCommands<SocialCommand>();
        foreach (var chunk in socialCommandDefinitions.OrderBy(x => x.Definition.Name).Chunk(4))
        {
            _msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x.Definition.Name,-14}")));
        }
    }
}
