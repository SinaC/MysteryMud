using Arch.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;

namespace MysteryMud.Infrastructure.Services;

public class ActMessageBuilder : IActMessageBuilder
{
    private readonly IMessageBus _messageBus;
    private readonly IActService _actService;
    private readonly IEnumerable<Entity> _targets;
    private readonly string _format;

    public ActMessageBuilder(IMessageBus messageBus,
        IActService actService,
        IEnumerable<Entity> targets,
        string format)
    {
        _targets = targets;
        _format = format;
        _messageBus = messageBus;
        _actService = actService;
    }

    public void With(params object[] args)
    {
        foreach (var target in _targets)
        {
            var text = _actService.FormatFor(target, _format, args);

            _messageBus.Publish(target, text);
        }
    }
}
