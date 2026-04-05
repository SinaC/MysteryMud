using Arch.Core;

namespace MysteryMud.Core.Commands;

public interface ICommand
{
    void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args);
}
