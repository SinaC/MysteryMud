using Arch.Core;

namespace MysteryMud.ConsoleApp3.Core.Scheduler;

public interface IScheduler
{
    public void Publish(Entity entity, ScheduledEventType type, long executedAt);
    public void Process(SystemContext ctx, GameState state);
}
