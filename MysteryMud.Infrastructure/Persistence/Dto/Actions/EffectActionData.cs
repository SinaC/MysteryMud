namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal abstract class EffectActionData
{
    public string Trigger { get; init; } = "OnApply";
}
