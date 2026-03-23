using Arch.Core;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Infrastructure.Services;

namespace MysteryMud.ConsoleApp3.Infrastructure.Eventing;

public class MessageBus : IMessageBus
{
    private readonly Queue<MessageEvent> _queue = new();

    private readonly IMessageService _messageService;

    public MessageBus(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public void Publish(Entity entity, string message)
    {
        _queue.Enqueue(new MessageEvent { Entity = entity, Message = message });
    }

    public void Process(SystemContext ctx, GameState gameState)
    {
        while (_queue.TryDequeue(out var evt))
        {
            _messageService.Send(evt.Entity, evt.Message);
        }
    }
}
