namespace MysteryMud.Infrastructure.Persistence.Dto;

public class EffectTemplateData
{
    public string Name { get; set; }
    public string Tag { get; set; }
    public string Stacking { get; set; }
    public int MaxStacks { get; set; } = 1;
    public List<string> Flags { get; set; } = new();
    public List<StatModifierDefinitionData> StatModifiers { get; set; } = new();
    public string DurationFormula { get; set; }
    public DotData Dot { get; set; }
    public HotData Hot { get; set; }
    public string ApplyMessage { get; set; }
    public string WearOffMessage { get; set; }
}
