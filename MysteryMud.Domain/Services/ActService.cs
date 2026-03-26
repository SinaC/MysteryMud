using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Formatters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Services;

public class ActService : IActService
{
    private readonly IMessageBus _messageBus;

    public ActService(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public void Send(Entity actor, ActTargetOptions option, string format, params object[]? args)
        => Send(actor, option, format, Positions.Resting, args);

    public void Send(Entity actor, ActTargetOptions option, string format, Positions minPosition = Positions.Resting, params object[]? args)
    {
        var victims = args?.OfType<Entity>().Where(x => x.Has<CharacterTag>())?.ToArray();
        var targets = ActFormatter.GetActTargets(option, actor, minPosition, victims);

        Send(targets, format, args);
    }

    public void Send(IEnumerable<Entity> targets, string format, params object[]? args)
    {
        foreach (var target in targets)
        {
            var phrase = ActFormatter.FormatActOneLine(target, format, args);

            _messageBus.Send(target, phrase);
        }
    }
}
