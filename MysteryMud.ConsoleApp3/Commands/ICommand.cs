using Arch.Core;

namespace MysteryMud.ConsoleApp3.Commands;

interface ICommand
{
    CommandParseMode ParseMode { get; }

    void Execute(Entity actor, CommandContext ctx);
}
