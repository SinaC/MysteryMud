using Arch.Core;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Core;

namespace MysteryMud.ConsoleApp3.Commands;

public interface ICommand
{
    //string Name { get; }
    //string[] Aliases { get; }

    CommandParseMode ParseMode { get; }

    //CommandLevel RequiredLevel { get; }
    //Position MinimumPosition { get; }

    //int Priority { get; }
    //bool AllowAbbreviation { get; }

    void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx);
}
