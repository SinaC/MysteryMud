using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;

namespace MysteryMud.Tests.Infrastructure;

internal class TestGameMessageService : IGameMessageService
{
    private readonly List<Message> Messages = [];

    public IMessageTargetBuilder To(Entity entity)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        Messages.Add(new Message { Recipient = entity, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder To(IEnumerable<Entity> entities)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        foreach(var entity in entities)
            Messages.Add(new Message { Recipient = entity, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToGroup(Entity group)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var groupInstance = ref group.TryGetRef<GroupInstance>(out var isGroup);
        if (isGroup)
        {
            foreach (var member in groupInstance.Members)
                Messages.Add(new Message { Recipient = member, MsgTargetBuilder = msgTargetBuilder });
        }
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToAll(Entity entity)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var people = ref entity.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach(var entry in people)
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToAllExcept(Entity entity, Entity except)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var people = ref entity.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach (var entry in people.Where(x => x != except))
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToRoom(Entity entity)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var people = ref entity.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach (var entry in people.Where(x => x != entity))
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToRoomExcept(Entity entity, Entity except)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var people = ref entity.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach (var entry in people.Where(x => x != entity && x != except))
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public bool HasMessageFor(Entity entity) =>
        Messages.Any(m => m.Recipient == entity);

    public List<string> GetMessagesFor(Entity entity)
        => [.. Messages.Where(x => x.Recipient == entity).Select(x => x.MsgTargetBuilder.Text)];

    private class Message
    {
        public Entity Recipient;
        public TestMessageTargetBuilder MsgTargetBuilder = default!;
    }
}
