using Arch.Core;
using MysteryMud.Application.Commands;
using MysteryMud.Application.Dispatching;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands
{
    public class SocialsCommand : ICommand
    {
        private readonly ICommandRegistry _commandRegistry;

        public CommandParseOptions ParseOptions => CommandParseOptions.None;

        public CommandDefinition Definition { get; } = new CommandDefinition
        {
            Name = "socials",
            Aliases = [],
            RequiredLevel = CommandLevels.Player,
            MinimumPosition = Positions.Dead,
            Priority = 0,
            AllowAbbreviation = true,
            HelpText = "Display list of available socials.",
            Syntaxes = ["[cmd]"],
            Categories = []
        };

        public SocialsCommand(ICommandRegistry commandRegistry)
        {
            _commandRegistry = commandRegistry;
        }

        public void Execute(SystemContext systemContext, GameState state, Entity actor, CommandContext ctx)
        {
            var socialCommandDefinitions = _commandRegistry.GetCommandDefinitions<SocialCommand>();
            foreach (var chunk in socialCommandDefinitions.OrderBy(x => x.Name).Chunk(4))
            {
                systemContext.Msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x.Name,-14}")));
            }
        }
    }
}
