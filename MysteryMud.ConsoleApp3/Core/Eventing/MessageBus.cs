using Arch.Core;
using Microsoft.Extensions.Logging;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public static class MessageBus
{
    private static readonly Queue<MessageEvent> _queue = new();

    public static void Publish(Entity entity, string message)
    {
        _queue.Enqueue(new MessageEvent { Entity = entity, Message = message });
    }

    public static void Process(GameState gameState)
    {
        while (_queue.TryDequeue(out var evt))
        {
            if (Services.Services.Messages == null)
            {
                Logger.Logger.System(LogLevel.Error, "[MessageSystem] No message service available to send message: {Message}", evt.Message);
                return;
            }
            Services.Services.Messages.Send(evt.Entity, evt.Message);
        }
    }
}
