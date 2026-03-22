namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public record ContainerArg
{
    public string Name { get; init; } = "";
    public int? EntityId { get; set; } = null; // resolved container entity
}
