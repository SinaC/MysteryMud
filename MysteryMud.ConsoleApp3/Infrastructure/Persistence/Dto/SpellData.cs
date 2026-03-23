namespace MysteryMud.ConsoleApp3.Infrastructure.Persistence.Dto;

public class SpellData
{
    public string Name { get; set; }
    public List<string> Effects { get; set; } = new();
}
