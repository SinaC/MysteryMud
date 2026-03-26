using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Domain.Formatters;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.Infrastructure.Eventing;

public class MessageBus : IMessageBus
{
    private readonly Queue<MessageEvent> _queue = new();

    private readonly IMessageService _messageService;

    public MessageBus(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public void Send(Entity entity, string message)
    {
        _queue.Enqueue(new MessageEvent { Entity = entity, Message = message });
    }

    public void Act(IEnumerable<Entity> entities, string format, params object[] arguments)
    {
        foreach (var target in entities)
        {
            var phrase = ActFormatter.FormatActOneLine(target, format, arguments);
            _queue.Enqueue(new MessageEvent { Entity = target, Message = phrase });
        }
    }

    public void Process(SystemContext ctx, GameState gameState)
    {
        while (_queue.TryDequeue(out var evt))
        {
            _messageService.Send(evt.Entity, evt.Message);
        }
    }
}
