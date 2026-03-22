using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems;

public static class MessageSystem
{
    public static void Send(Entity entity, string message)
    {
        if (Services.Services.Messages == null)
        {
            Console.WriteLine($"[MessageSystem] No message service available to send message: {message}");
            return;
        }
        Services.Services.Messages.Send(entity, message);
    }

    public static void Broadcast(Entity room, string message)
    {
        if (Services.Services.Messages == null)
        {
            Console.WriteLine($"[MessageSystem] No message service available to broadcast message: {message}");
            return;
        }

        ref var roomContents = ref room.TryGetRef<RoomContents>(out var hasRoomContents);
        if (hasRoomContents)
        {
            foreach (var character in roomContents.Characters.Where(x => !x.Has<Dead>()))
            {
                // Append message to character's output buffer
                Services.Services.Messages?.Send(character, message);
            }
        }
    }
}
