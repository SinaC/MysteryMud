using Arch.Core;

namespace MysteryMud.Core.Command;

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
