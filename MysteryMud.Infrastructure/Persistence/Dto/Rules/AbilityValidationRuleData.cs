namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

public abstract class AbilityValidationRuleData
{
    public required string Scope { get; init; }
    public string OnFail { get; init; } = default!;
    public string Fail { get; init; } = default!; // message to be displayed if failed (only if OnFail is not SkipTarget)
}
