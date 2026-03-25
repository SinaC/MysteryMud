using Arch.Core;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Core.Command;

public interface ICommand
{
    CommandParseOptions ParseOptions { get; }
    CommandDefinition Definition { get; }

    void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx);
}
