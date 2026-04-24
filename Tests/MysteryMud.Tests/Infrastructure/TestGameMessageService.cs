using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Tests.Infrastructure;

internal class TestGameMessageService : IGameMessageService
{
    private readonly List<Message> Messages = [];

    private readonly World _world;

    public TestGameMessageService(World world)
    {
        _world = world;
    }

    public IMessageTargetBuilder To(EntityId entity)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        Messages.Add(new Message { Recipient = entity, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder To(IEnumerable<EntityId> entities)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        foreach(var entity in entities)
            Messages.Add(new Message { Recipient = entity, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToGroup(EntityId group)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var groupInstance = ref _world.TryGetRef<GroupInstance>(group, out var isGroup);
        if (isGroup)
        {
            foreach (var member in groupInstance.Members)
                Messages.Add(new Message { Recipient = member, MsgTargetBuilder = msgTargetBuilder });
        }
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToAll(EntityId entity)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var room = ref _world.Get<Location>(entity).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;
        foreach(var entry in people)
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToAllExcept(EntityId entity, EntityId except)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var room = ref _world.Get<Location>(entity).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;
        foreach (var entry in people.Where(x => x != except))
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToRoom(EntityId entity)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var room = ref _world.Get<Location>(entity).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;
        foreach (var entry in people.Where(x => x != entity))
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public IMessageTargetBuilder ToRoomExcept(EntityId entity, EntityId except)
    {
        var msgTargetBuilder = new TestMessageTargetBuilder();
        ref var room = ref _world.Get<Location>(entity).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;
        foreach (var entry in people.Where(x => x != entity && x != except))
            Messages.Add(new Message { Recipient = entry, MsgTargetBuilder = msgTargetBuilder });
        return msgTargetBuilder;
    }

    public bool HasMessageFor(EntityId entity) =>
        Messages.Any(m => m.Recipient == entity);

    public List<string> GetMessagesFor(EntityId entity)
        => [.. Messages.Where(x => x.Recipient == entity).Select(x => x.MsgTargetBuilder.Text)];

    private class Message
    {
        public EntityId Recipient;
        public TestMessageTargetBuilder MsgTargetBuilder = default!;
    }
}
