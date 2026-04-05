using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Commands;

namespace MysteryMud.Application.Dispatching;

public interface ICommandDispatcher
{
    void Dispatch(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> input);
}
