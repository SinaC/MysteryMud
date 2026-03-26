namespace MysteryMud.Core.Scheduler;

public interface IScheduler : ISchedule
{
    public void Process(SystemContext ctx, GameState state);
}
