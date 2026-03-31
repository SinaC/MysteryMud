namespace MysteryMud.GameData.Definitions;

public class SpellDefinition
{
    public required string Name { get; init; }
    // TODO: direct damage/heal
    public required EffectDefinition[] Effects { get; init; }
}
