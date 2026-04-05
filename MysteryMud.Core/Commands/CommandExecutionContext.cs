using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;

namespace MysteryMud.Core.Commands;

public class CommandExecutionContext
{
    public required IGameMessageService Msg { get; init; }
    public required IIntentWriterContainer Intent { get; init; }
}
