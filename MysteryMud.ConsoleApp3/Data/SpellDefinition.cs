using MysteryMud.ConsoleApp3.Data.EffectTemplates;

namespace MysteryMud.ConsoleApp3.Data
{
    public class SpellDefinition
    { 
        public int Id;
        public string Name;
        public int Duration; // Duration in ticks
        public string ApplyMessage;
        public string WearOffMessage;
        public List<IEffectTemplate> EffectTemplates = new();
    }
}
