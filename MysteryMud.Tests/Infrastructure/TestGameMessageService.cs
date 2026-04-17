using Arch.Core;
using MysteryMud.Domain.Services;

namespace MysteryMud.Tests.Infrastructure;

internal class TestGameMessageService : IGameMessageService
{
    public IMessageTargetBuilder To(Entity entity)
        => new TestMessageTargetBuilder();

    public IMessageTargetBuilder To(IEnumerable<Entity> entities)
        => new TestMessageTargetBuilder();

    public IMessageTargetBuilder ToAll(Entity entity)
        => new TestMessageTargetBuilder();

    public IMessageTargetBuilder ToAllExcept(Entity entity, Entity except)
        => new TestMessageTargetBuilder();

    public IMessageTargetBuilder ToRoom(Entity entity)
        => new TestMessageTargetBuilder();

    public IMessageTargetBuilder ToRoomExcept(Entity entity, Entity except)
        => new TestMessageTargetBuilder();
}
