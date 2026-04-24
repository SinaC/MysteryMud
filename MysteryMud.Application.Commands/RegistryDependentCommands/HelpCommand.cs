using MysteryMud.Application.Parsing;
using MysteryMud.Application.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.RegistryDependentCommands;

public sealed class HelpCommand : IExplicitCommand
{
    private const string Name = "help";
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly ICommandRegistry _commandRegistry;
    private readonly IGameMessageService _msg;

    public CommandDefinition Definition { get; } = new CommandDefinition
    {
        Id = Name.ComputeUniqueId(),
        Name = Name,
        Aliases = ["?"],
        CannotBeForced = true,
        RequiredLevel = CommandLevelKind.Player,
        MinimumPosition = PositionKind.Dead,
        Priority = 0,
        DisallowAbbreviation = false,
        HelpText = "[cmd] shows you commands in a category, all categories or all commands starting with a prefix.",
        Syntaxes = ["[cmd]", "[cmd] <prefix>", "[cmd] <category>"],
        Categories = ["information"],
        ThrottlingCategories = CommandThrottlingCategories.Utility,
    };

    public HelpCommand(ICommandRegistry commandRegistry, IGameMessageService msg)
    {
        _commandRegistry = commandRegistry;
        _msg = msg;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            var commands = _commandRegistry.GetCommands(CommandLevelKind.Player); // TODO: CommandLevel should be determined by actor's actual level, not just Player
            _msg.To(actor).Send("Available command categories:%W");
            foreach (var chunk in commands.Where(x => x.Definition.Categories.Length > 0).SelectMany(cmd => cmd.Definition.Categories).Distinct().OrderBy(x => x).Chunk(4))
            {
                _msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x,-14}")));
            }
            _msg.To(actor).Send("%xNo category:%W");
            foreach (var chunk in commands.Where(cmd => cmd.Definition.Categories.Length == 0).Select(cmd => cmd.Definition.Name).OrderBy(x => x).Chunk(4))
            {
                _msg.To(actor).Send(string.Join(string.Empty, chunk.Select(x => $"{x,-14}")));
            }
            _msg.To(actor).Send("%xType 'help <category>' to see commands in that category, or 'help <prefix>' to search for commands starting with that prefix.");
        }
        else
        {
            var arg = ctx.Primary.Name.ToString();
            var commandsByCategory = _commandRegistry.GetCommands(CommandLevelKind.Player)
                .Where(cmd => cmd.Definition.Name.StartsWith(arg, StringComparison.OrdinalIgnoreCase) || cmd.Definition.Categories.Contains(arg, StringComparer.OrdinalIgnoreCase))
                .GroupBy(cmd => cmd.Definition.Categories.FirstOrDefault(c => c.Equals(arg, StringComparison.OrdinalIgnoreCase)) ?? "uncategorized");
            foreach (var group in commandsByCategory)
            {
                _msg.To(actor).Send($"Category: {group.Key}");
                foreach (var item in group)
                {
                    _msg.To(actor).Send($"  {item.Definition.Name} -  %#FA8640>#0486FA{item.Definition.HelpText}%x");
                }
            }
        }
    }
}
