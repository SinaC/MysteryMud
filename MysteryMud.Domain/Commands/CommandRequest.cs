using MysteryMud.Core.Commands;

namespace MysteryMud.Domain.Commands;

public struct CommandRequest
{
    public RegisteredCommand Command;
    public int CommandId;

    public string Input; // full input string (1 allocation max)
    public int CmdStart;
    public int CmdLength;
    public int ArgsStart;
    public int ArgsLength;

    public long ExecuteAt; // for lag scheduling

    public bool Cancelled;
    public bool Force; // skip spam/cooldown/wait

    public readonly ReadOnlySpan<char> CommandSpan
        => Input.AsSpan(CmdStart, CmdLength);

    public readonly ReadOnlySpan<char> ArgsSpan
        => Input.AsSpan(ArgsStart, ArgsLength);
}
