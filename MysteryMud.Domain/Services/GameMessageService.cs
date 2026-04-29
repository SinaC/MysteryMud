using DefaultEcs;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Formatters;

namespace MysteryMud.Domain.Services;

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

    public IMessageTargetBuilder ToGroup(Entity group)
    {
        var targets = ActTargetResolver.GetGroupTargets(group);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to everyone in the room except actor
    public IMessageTargetBuilder ToRoom(Entity entity)
    {
        var targets = ActTargetResolver.GetRoomTargets(entity);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to everyone in the room except 'actor' and 'except'
    public IMessageTargetBuilder ToRoomExcept(Entity entity, Entity except)
    {
        var targets = ActTargetResolver.GetRoomTargetsExcept(entity, except);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to everyone in the room
    public IMessageTargetBuilder ToAll(Entity entity)
    {
        var targets = ActTargetResolver.GetAllTargets(entity);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to every in the room except 'except'
    public IMessageTargetBuilder ToAllExcept(Entity entity, Entity except)
    {
        var targets = ActTargetResolver.GetAllTargetsExcept(entity, except);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }
}
