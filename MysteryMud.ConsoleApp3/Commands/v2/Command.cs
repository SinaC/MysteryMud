namespace MysteryMud.ConsoleApp3.Commands.v2;

public class Command
{
    public string Name { get; set; }
    public string[] Syntaxes { get; set; }
    public Action<CommandContext> Execute { get; set; }
}
