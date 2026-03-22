namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class Command
{
    public string Name { get; set; }
    public int Priority { get; set; }
    public string[] Syntaxes { get; set; }
    public Action<CommandContext> Execute { get; set; }
}
