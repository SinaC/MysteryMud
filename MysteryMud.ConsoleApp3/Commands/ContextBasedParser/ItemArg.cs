namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public record ItemArg
{
    public bool AllOf { get; init; } = false;
    public bool All { get; init; } = false;
    public int? Index { get; init; } = null;
    public string Name { get; init; } = "";

    public override string ToString()
    {
        if (All) return "all";
        if (AllOf) return $"all.{Name}";
        if (Index.HasValue) return $"{Index}.{Name}";
        return Name;
    }
}
