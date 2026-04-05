namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

public abstract class EffectActionData
{
    public string Trigger { get; init; } = "OnApply";
}
