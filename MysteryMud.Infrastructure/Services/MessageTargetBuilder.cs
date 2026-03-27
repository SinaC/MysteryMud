using Arch.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;

namespace MysteryMud.Infrastructure.Services;

public class MessageTargetBuilder : IMessageTargetBuilder
{
    private readonly IMessageBus _messageBus;
    private readonly IActService _actService;
    private readonly IEnumerable<Entity> _targets;

    public MessageTargetBuilder(IMessageBus messageBus,
        IActService actService,
        IEnumerable<Entity> targets)
    {
        _messageBus = messageBus;
        _actService = actService;
        _targets = targets;
    }

    public void Send(string text)
    {
        foreach (var target in _targets)
        {
            _messageBus.Publish(target, text);
        }
    }

    public IActMessageBuilder Act(string format)
        => new ActMessageBuilder(_messageBus, _actService, _targets, format);
}
