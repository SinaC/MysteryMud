using MysteryMud.Core.Command;

namespace MysteryMud.Infrastructure.Command;

public class CommandFindResult
{
    public CommandFindResultType Type { get; }
    public ICommand? Command { get; }

    private CommandFindResult(CommandFindResultType type, ICommand? command = null)
    {
        Type = type;
        Command = command;
    }

    public static CommandFindResult Success(ICommand cmd) =>
        new(CommandFindResultType.Success, cmd);

    public static CommandFindResult Fail(CommandFindResultType type) =>
        new(type);
}
