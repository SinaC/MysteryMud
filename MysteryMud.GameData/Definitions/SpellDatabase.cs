namespace MysteryMud.GameData.Definitions;

public class SpellDatabase
{
    public Dictionary<string, SpellDefinition> Spells = []; // TODO: ReadOnlySpan<char>
    public Dictionary<int, SpellDefinition> SpellsById = [];
    public Dictionary<string, EffectDefinition> Effects = []; // TODO: ReadOnlySpan<char>
    public Dictionary<int, EffectDefinition> EffectsById = [];
}
