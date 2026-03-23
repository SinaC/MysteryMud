using Arch.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;

namespace MysteryMud.ConsoleApp3.Core.Scheduler;

public interface IScheduler
{
    public void Publish(Entity entity, ScheduledEventType type, long executedAt);
    public void Process(SystemContext systemContext, GameState state);
}
