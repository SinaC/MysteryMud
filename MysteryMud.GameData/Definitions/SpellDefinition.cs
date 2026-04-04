namespace MysteryMud.GameData.Definitions;

public class SpellDefinition
{
    public required int Id { get; init; } // generated
    public required string Name { get; init; }
    // TODO: direct damage/heal
    public required EffectDefinition[] Effects { get; init; }
}
