using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters.Players;
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
        ref var groupData = ref group.TryGetRef<Group>(out var isGroup);
        if (isGroup)
        {
            foreach (var member in groupData.Members)
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

    private class Message
    {
        public Entity Recipient;
        public TestMessageTargetBuilder MsgTargetBuilder = default!;
    }
}
