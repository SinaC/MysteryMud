using Arch.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Formatters;

namespace MysteryMud.Infrastructure.Services;

public class GameMessageService : IGameMessageService
{
    private readonly IMessageBus _messageBus;
    private readonly IActService _actService;

    public GameMessageService(IMessageBus bus, IActService act)
    {
        _messageBus = bus;
        _actService = act;
    }

    public IMessageTargetBuilder To(Entity entity)
        => new MessageTargetBuilder(_messageBus, _actService, [entity]);

    public IMessageTargetBuilder To(IEnumerable<Entity> entities)
        => new MessageTargetBuilder(_messageBus, _actService, entities);

    public IMessageTargetBuilder ToGroup(Entity entity)
    {
        var targets = ActTargetResolver.GetGroupTargets(entity);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    public IMessageTargetBuilder ToRoom(Entity entity)
    {
        var targets = ActTargetResolver.GetRoomTargets(entity);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    public IMessageTargetBuilder ToRoomExcept(Entity entity, Entity except)
    {
        var targets = ActTargetResolver.GetRoomTargetsExcept(entity, except);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    public IMessageTargetBuilder ToAll(Entity entity)
    {
        var targets = ActTargetResolver.GetAllTargets(entity);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
}
}
