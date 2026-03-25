namespace MysteryMud.GameData.Definitions;

public class SpellDatabase
{
    public Dictionary<string, SpellDefinition> Spells = []; // TODO: ReadOnlySpan<char>
    public Dictionary<string, EffectTemplate> EffectTemplates = [];
}
