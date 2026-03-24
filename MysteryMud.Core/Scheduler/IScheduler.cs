using Arch.Core;

namespace MysteryMud.Core.Scheduler;

public interface IScheduler
{
    public void Schedule(Entity entity, ScheduledEventType type, long executedAt);
    public void Process(SystemContext ctx, GameState state);
}
