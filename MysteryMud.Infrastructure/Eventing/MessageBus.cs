using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.Infrastructure.Eventing;

public class MessageBus : IMessageBus
{
    private readonly Queue<MessageEvent> _queue = new();

    private readonly IOutputService _outputService;

    public MessageBus(IOutputService outputService)
    {
        _outputService = outputService;
    }

    public void Publish(Entity entity, string message)
    {
        _queue.Enqueue(new MessageEvent { Entity = entity, Message = message });
    }

    public void Process(GameState state)
    {
        while (_queue.TryDequeue(out var evt))
        {
            _outputService.Send(evt.Entity, evt.Message);
        }
    }
}
