using TinyECS;

namespace MysteryMud.Application.Dispatching;

public interface ICommandDispatcher
{
    void Dispatch(EntityId actor, ReadOnlySpan<char> input);
}
