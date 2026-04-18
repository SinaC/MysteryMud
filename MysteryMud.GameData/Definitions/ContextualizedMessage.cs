namespace MysteryMud.GameData.Definitions;

public class ContextualizedMessage
{
    public string? ToActor { get; set; }
    public string? ToTarget { get; set; }
    public string? ToRoom { get; set; }
}
