using TinyECS;

namespace MysteryMud.Domain.Services;

public interface IGameMessageService
{
    IMessageTargetBuilder To(EntityId entity);
    IMessageTargetBuilder To(IEnumerable<EntityId> entities);
    IMessageTargetBuilder ToGroup(EntityId group); // to everyone in the group
    IMessageTargetBuilder ToRoom(EntityId entity); // to everyone in the room except actor
    IMessageTargetBuilder ToRoomExcept(EntityId entity, EntityId except); // to everyone in the room except 'actor' and 'except'
    IMessageTargetBuilder ToAll(EntityId entity); // to everyone in the room
    IMessageTargetBuilder ToAllExcept(EntityId entity, EntityId except); // to every in the room except 'except'
}
