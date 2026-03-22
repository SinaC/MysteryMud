using Arch.Core;

namespace MysteryMud.ConsoleApp3.Commands.v2;

public class CommandContext
{
    public Dictionary<string, object> Arguments { get; set; }
    public string RawInput { get; set; }
    public string CommandName { get; set; }
    public Command Command { get; set; }

    // The entity that issued the command
    public Entity Actor { get; set; }

    // The syntax pattern that matched
    public string MatchedSyntax { get; set; }
}
