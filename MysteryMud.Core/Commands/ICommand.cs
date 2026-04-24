using TinyECS;

namespace MysteryMud.Core.Commands;

public interface ICommand
{
    void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args);
}
