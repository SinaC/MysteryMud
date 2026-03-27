using Arch.Core;

namespace MysteryMud.Core.Services;

public interface IGameMessageService
{
    IMessageTargetBuilder To(Entity entity);
    IMessageTargetBuilder To(IEnumerable<Entity> entities);
    IMessageTargetBuilder ToRoom(Entity actor);
    IMessageTargetBuilder ToRoomExcept(Entity actor, Entity except);
    IMessageTargetBuilder ToAll(Entity actor);
}
