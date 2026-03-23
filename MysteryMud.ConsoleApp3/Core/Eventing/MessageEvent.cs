using Arch.Core;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public class MessageEvent
{
    public required Entity Entity { get; init; }
    public required string Message { get; init; }
}
