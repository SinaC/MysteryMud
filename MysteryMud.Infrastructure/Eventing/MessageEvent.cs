using TinyECS;

namespace MysteryMud.Infrastructure.Eventing;

public class MessageEvent
{
    public required EntityId Entity { get; init; }
    public required string Message { get; init; }
}
