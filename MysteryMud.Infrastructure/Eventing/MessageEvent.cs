using DefaultEcs;

namespace MysteryMud.Infrastructure.Eventing;

public class MessageEvent
{
    public required Entity Entity { get; init; }
    public required string Message { get; init; }
}
