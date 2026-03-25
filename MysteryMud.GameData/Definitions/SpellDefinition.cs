namespace MysteryMud.GameData.Definitions;

public class SpellDefinition
{
    public required string Name { get; init; }
    // TODO: direct damage/heal
    public required EffectTemplate[] Effects { get; init; }
}
