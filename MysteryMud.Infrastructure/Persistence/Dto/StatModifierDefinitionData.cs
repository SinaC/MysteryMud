namespace MysteryMud.Infrastructure.Persistence.Dto;

public class StatModifierDefinitionData
{
    public string Stat { get; set; }
    public string Type { get; set; }
    public int Value { get; set; } // TODO: formula ?
}
