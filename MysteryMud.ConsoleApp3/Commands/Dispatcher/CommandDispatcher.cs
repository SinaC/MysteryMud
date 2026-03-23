using Arch.Core;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Commands.Registry;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Commands.Dispatcher;

class CommandDispatcher
{
    public static void Dispatch(GameState gameState, Entity actor, ReadOnlySpan<char> input)
    {
        Console.WriteLine($"*** [{actor.DisplayName}] EXECUTING [{input}]");

        // extract command and arguments
        CommandParser.SplitCommand(input, out var cmdSpan, out var argsSpan);

        // search command in registry
        if (!CommandRegistry.TryGet(cmdSpan, out var cmd))
        {
            MessageBus.Publish(actor, "Unknown command.");
            return;
        }

        // parse arguments using command-specific rules
        CommandParser.Parse(cmd.ParseMode, cmdSpan, argsSpan, out var ctx);

        // execute command
        cmd.Execute(gameState, actor, ctx);
    }
}
