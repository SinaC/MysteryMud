using Arch.Core;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Core;

namespace MysteryMud.ConsoleApp3.Commands;

interface ICommand
{
    //string Name { get; }
    //string[] Aliases { get; }

    CommandParseMode ParseMode { get; }

    //CommandLevel RequiredLevel { get; }
    //Position MinimumPosition { get; }

    //int Priority { get; }
    //bool AllowAbbreviation { get; }

    void Execute(GameState gameState, Entity actor, CommandContext ctx);
}
