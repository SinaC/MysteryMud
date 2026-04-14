namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class CharacterStatModifierData : EffectActionData
{
    public required string Stat { get; init; }
    public required string Mode { get; init; }
    public required string ValueFormula { get; init; }
}
