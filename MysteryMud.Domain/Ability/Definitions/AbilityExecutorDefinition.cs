using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class AbilityExecutorDefinition
{
    public required string Executor { get; init; } = "default";
    public required AbilityExecutorHook Hook { get; init; }
}
