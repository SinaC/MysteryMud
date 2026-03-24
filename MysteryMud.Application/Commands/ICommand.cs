using Arch.Core;
using MysteryMud.Application.Commands.Parser;
using MysteryMud.Core;

namespace MysteryMud.Application.Commands;

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
