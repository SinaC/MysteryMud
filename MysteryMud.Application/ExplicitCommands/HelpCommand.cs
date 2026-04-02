using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

// this is a special case
public class HelpCommand : ICommand
{
    private const string Name = "help";
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly ICommandRegistry _commandRegistry;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeCommandId(),
        Name = Name,
        Aliases = ["?"],
        CannotBeForced = true, // TODO: test
        RequiredLevel = CommandLevelKind.Player,
        MinimumPosition = PositionKind.Dead,
        Priority = 0,
        AllowAbbreviation = true,
        HelpText = "[cmd] shows you commands in a category, all categories or all commands starting with a prefix.",
        Syntaxes = ["[cmd]", "[cmd] <prefix>", "[cmd] <category>"],
        Categories = []
    };

    public HelpCommand(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            var commands = _commandRegistry.GetCommandDefinitions(CommandLevelKind.Player); // TODO: CommandLevel should be determined by actor's actual level, not just Player
            systemContext.Msg.To(actor).Send("Available command categories:%W");
            foreach (var chunk in commands.Where(x => x.Categories.Length > 0).SelectMany(cmd => cmd.Categories).Distinct().OrderBy(x => x).Chunk(4))
            {
                systemContext.Msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x,-14}")));
            }
            systemContext.Msg.To(actor).Send("%xNo category:%W");
            foreach(var chunk in commands.Where(cmd => cmd.Categories.Length == 0).Select(cmd => cmd.Name).OrderBy(x => x).Chunk(4))
            {
                systemContext.Msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x,-14}")));
            }
            systemContext.Msg.To(actor).Send("%xType 'help <category>' to see commands in that category, or 'help <prefix>' to search for commands starting with that prefix.");
        }
        else
        {
            var arg = ctx.Primary.Name.ToString();
            var commandsByCategory = _commandRegistry.GetCommandDefinitions(CommandLevelKind.Player)
                .Where(cmd => cmd.Name.StartsWith(arg, StringComparison.OrdinalIgnoreCase) || cmd.Categories.Contains(arg, StringComparer.OrdinalIgnoreCase))
                .GroupBy(cmd => cmd.Categories.FirstOrDefault(c => c.Equals(arg, StringComparison.OrdinalIgnoreCase)) ?? "uncategorized");
            foreach (var group in commandsByCategory)
            {
                systemContext.Msg.To(actor).Send($"Category: {group.Key}");
                foreach (var item in group)
                {
                    systemContext.Msg.To(actor).Send($"  {item.Name} -  %#FA8640>#0486FA{item.HelpText}");
                }
            }
        }
    }
}
