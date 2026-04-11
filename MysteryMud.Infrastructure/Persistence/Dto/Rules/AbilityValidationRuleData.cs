namespace MysteryMud.Infrastructure.Persistence.Dto.Rules;

public abstract class AbilityValidationRuleData
{
    public string OnFail { get; init; } = default!; // Abort/Skip/SkipWithMessage
    public string MessageKey { get; init; } = default!; // key of message to be displayed in case of fail
}
