namespace MysteryMud.Infrastructure.Persistence.Dto.Actions;

internal class CharacterIRVModifierData : EffectActionData
{
    public required string Mode { get; init; }
    public required string Location { get; init; }
    public required string DamageKinds { get; init; }
}
