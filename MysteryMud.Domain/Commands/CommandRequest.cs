using MysteryMud.Core.Commands;

namespace MysteryMud.Domain.Commands;

public struct CommandRequest
{
    public ICommand Command; // TODO: replace with command id and assign command id to command
    public int CommandId; // TODO: assign command id to command

    public string RawCommand;
    public string RawArgs;

    public bool Cancelled;
    public bool Force; // skip spam/cooldown/wait
}
