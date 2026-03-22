using Arch.Core;

namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class CommandContext
{
    public Dictionary<string, ArgValue> Arguments { get; set; }
    public string RawInput { get; set; }
    public Command Command { get; set; }

    public World World { get; set; }
    public Entity Actor { get; set; }

    public Syntax MatchedSyntax { get; set; }
}
