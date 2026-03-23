using MysteryMud.ConsoleApp3.Core.Eventing;

namespace MysteryMud.ConsoleApp3.Core;

public class SystemContext
{
    public required ICommandBus CommandBus { get; init; }
    public required IMessageBus MessageBus { get; init; }
    public required IScheduler Scheduler { get; init; }
}
