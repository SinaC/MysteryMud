namespace MysteryMud.Infrastructure.Persistence.Dto.Effects;

public abstract class EffectActionData
{
    public string Trigger { get; init; } = "OnApply";
}
