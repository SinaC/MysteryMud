using Arch.Core;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

class CommandDispatcher
{
    public static void Dispatch(World world, Entity actor, ReadOnlySpan<char> input)
    {
        Console.WriteLine($"*** [{actor.DisplayName}] EXECUTING [{input}]");

        // extract command and arguments
        CommandParser.SplitCommand(input, out var cmdSpan, out var argsSpan);

        // search command in registry
        if (!CommandRegistry.TryGet(cmdSpan, out var cmd))
        {
            MessageSystem.Send(actor, "Unknown command.");
            return;
        }

        // parse arguments using command-specific rules
        CommandParser.Parse(cmd.ParseMode, cmdSpan, argsSpan, out var ctx);

        // execute command
        cmd.Execute(world, actor, ctx);
    }
}
