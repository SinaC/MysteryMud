using Arch.Core;

namespace MysteryMud.Core.Scheduler;

public interface ISchedule
{
    public void Schedule(Entity entity, ScheduledEventType type, long executedAt);

}
