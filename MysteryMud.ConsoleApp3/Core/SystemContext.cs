using Microsoft.Extensions.Logging;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Core.Scheduler;

namespace MysteryMud.ConsoleApp3.Core;

public class SystemContext
{
    public required ILogger Log { get; init; }
    public required ICommandBus CommandBus { get; init; }
    public required IMessageBus MessageBus { get; init; }
    public required IScheduler Scheduler { get; init; }
}
