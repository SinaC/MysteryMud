using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public interface ICommand
{
    CommandParseOptions ParseOptions { get; }
    CommandDefinition Definition { get; }

    void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx);
}
