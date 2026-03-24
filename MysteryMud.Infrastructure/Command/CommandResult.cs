using MysteryMud.Core.Command;

namespace MysteryMud.Infrastructure.Command;

class CommandResult
{
    public CommandResultType Type { get; }
    public ICommand? Command { get; }

    private CommandResult(CommandResultType type, ICommand? command = null)
    {
        Type = type;
        Command = command;
    }

    public static CommandResult Success(ICommand cmd) =>
        new(CommandResultType.Success, cmd);

    public static CommandResult Fail(CommandResultType type) =>
        new(type);
}
