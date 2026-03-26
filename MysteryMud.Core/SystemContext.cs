using Microsoft.Extensions.Logging;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Scheduler;
using MysteryMud.Core.Services;

namespace MysteryMud.Core;

public class SystemContext
{
    public required ILogger Log { get; init; }
    public required IMessageWriter Msg { get; init; }
    public required ISchedule Scheduler { get; init; }
    public required IActService Act { get; init; }
}
