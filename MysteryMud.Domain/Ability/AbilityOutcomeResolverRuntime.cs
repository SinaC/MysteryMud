using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability;

public class AbilityOutcomeResolverRuntime
{
    public int ResolverId { get; init; }
    public AbilityOutcomeHook Hook { get; init; }
}
