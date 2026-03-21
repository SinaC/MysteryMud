namespace MysteryMud.ConsoleApp3.Commands.v2;

public record ItemArg
{
    public bool All { get; init; } = false;
    public int? Index { get; init; } = null;
    public string Name { get; init; } = "";
    public List<int> Entities { get; set; } = new(); // resolved entity IDs
}
