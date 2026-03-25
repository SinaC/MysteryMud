using Arch.Core;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Core.Command;

public interface ICommand
{
    CommandParseOptions ParseOptions { get; }
    CommandDefinition Definition { get; }

    void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx);

    static readonly CommandParseOptions None = new(0, false);
    static readonly CommandParseOptions FullText = new(0, true);
    static readonly CommandParseOptions Target = new(1, false);
    static readonly CommandParseOptions TargetAndText = new(1, true);
    static readonly CommandParseOptions TargetPair = new(2, false);
    static readonly CommandParseOptions TargetPairAndText = new(2, true);
    static readonly CommandParseOptions TargetTriple = new(3, false);
}
