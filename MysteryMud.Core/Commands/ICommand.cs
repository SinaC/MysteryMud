using Arch.Core;

namespace MysteryMud.Core.Commands;

public interface ICommand
{
    void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args);
}
