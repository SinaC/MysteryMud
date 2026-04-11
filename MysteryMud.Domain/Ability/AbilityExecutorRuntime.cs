using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability;

public class AbilityExecutorRuntime
{
    public int ExecutorId { get; init; }
    public AbilityExecutorHook Hook { get; init; }
}
