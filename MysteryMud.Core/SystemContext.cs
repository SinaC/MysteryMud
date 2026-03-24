using Microsoft.Extensions.Logging;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Scheduler;

namespace MysteryMud.Core;

public class SystemContext
{
    public required ILogger Log { get; init; }
    public required IMessageBus MessageBus { get; init; }
    public required IScheduler Scheduler { get; init; }
}
