namespace MysteryMud.Domain.Data.Definitions;

public class SpellDefinition
{
    public string Name = default!;
    // TODO: direct damage/heal
    public EffectTemplate[] Effects = [];
}
