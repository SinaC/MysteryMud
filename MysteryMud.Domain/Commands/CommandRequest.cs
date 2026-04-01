using MysteryMud.Core.Commands;

namespace MysteryMud.Domain.Commands;

public struct CommandRequest
{
    public ICommand Command;
    public int CommandId;

    public string RawCommand;
    public string RawArgs;

    public bool Cancelled;
    public bool Force; // skip spam/cooldown/wait
}
