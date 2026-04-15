using Arch.Core;

namespace MysteryMud.Domain.Services;

public interface IGameMessageService
{
    IMessageTargetBuilder To(Entity entity);
    IMessageTargetBuilder To(IEnumerable<Entity> entities);
    IMessageTargetBuilder ToRoom(Entity entity); // to everyone in the room except actor
    IMessageTargetBuilder ToRoomExcept(Entity entity, Entity except); // to everyone in the room except 'actor' and 'except'
    IMessageTargetBuilder ToAll(Entity entity); // to everyone in the room
    IMessageTargetBuilder ToAllExcept(Entity entity, Entity except); // to every in the room except 'except'
}
