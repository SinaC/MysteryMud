using MysteryMud.Core.Bus;
using TinyECS;

namespace MysteryMud.Domain.Services;

public class GameMessageService : IGameMessageService
{
    private readonly World _world;
    private readonly IMessageBus _messageBus;
    private readonly IActService _actService;

    public GameMessageService(World world, IMessageBus bus, IActService act)
    {
        _world = world;
        _messageBus = bus;
        _actService = act;
    }

    public IMessageTargetBuilder To(EntityId entity)
        => new MessageTargetBuilder(_messageBus, _actService, [entity]);

    public IMessageTargetBuilder To(IEnumerable<EntityId> entities)
        => new MessageTargetBuilder(_messageBus, _actService, entities);

    public IMessageTargetBuilder ToGroup(EntityId group)
    {
        var targets = ActTargetResolver.GetGroupTargets(_world, group);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to everyone in the room except actor
    public IMessageTargetBuilder ToRoom(EntityId entity)
    {
        var targets = ActTargetResolver.GetRoomTargets(_world, entity);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to everyone in the room except 'actor' and 'except'
    public IMessageTargetBuilder ToRoomExcept(EntityId entity, EntityId except)
    {
        var targets = ActTargetResolver.GetRoomTargetsExcept(_world, entity, except);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to everyone in the room
    public IMessageTargetBuilder ToAll(EntityId entity)
    {
        var targets = ActTargetResolver.GetAllTargets(_world, entity);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }

    // to every in the room except 'except'
    public IMessageTargetBuilder ToAllExcept(EntityId entity, EntityId except)
    {
        var targets = ActTargetResolver.GetAllTargetsExcept(_world, entity, except);
        return new MessageTargetBuilder(_messageBus, _actService, targets);
    }
}
