namespace MysteryMud.ConsoleApp3.Commands.v2;

public record ContainerArg
{
    public string Name { get; init; } = "";
    public int? EntityId { get; set; } = null; // resolved container entity
}
