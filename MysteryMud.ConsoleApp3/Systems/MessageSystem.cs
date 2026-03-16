using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Networking;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

public static class MessageSystem
{
    public static void SendMessage(Entity entity, string message)
    {
        if (entity.Has<Connection>())
        {
            var conn = entity.Get<Connection>();
            conn.Value.Write(message + "\r\n");
        }
        else
        {
            Console.WriteLine($"[{entity.DisplayName}]: {message}");
        }
    }

    // TODO: colors will be handled
    //public static void SendMessage(Entity entity, ReadOnlySpan<byte> message)
    //{
    //    if (entity.Has<Connection>())
    //    {
    //        var conn = entity.Get<Connection>();
    //        conn.Value.WriteBytes(message);
    //    }
    //    else
    //    {
    //        Console.Write($"[{entity.Get<Name>().Value}]: {System.Text.Encoding.UTF8.GetString(message)}");
    //    }
    //}
}
