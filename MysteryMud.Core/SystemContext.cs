using Microsoft.Extensions.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Core.Services;

namespace MysteryMud.Core;

public class SystemContext
{
    public required ILogger Log { get; init; }
    public required IGameMessageService Msg { get; init; }
    public required IScheduler Scheduler { get; init; }
}
