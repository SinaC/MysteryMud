using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Components.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

// TODO: see Mud.Net
public static class ActSystem
{
    // tokens: $n = actor, $N = target, $e = actor subject pronoun, $E = target subject pronoun
    public static string Format(string message, Entity actor, Entity target)
    {
        Span<char> buffer = stackalloc char[512]; // stack buffer to avoid heap allocations
        int pos = 0;

        for (int i = 0; i < message.Length; i++)
        {
            if (message[i] == '$' && i + 1 < message.Length)
            {
                i++;
                string replacement = message[i] switch
                {
                    'n' => actor.DisplayName,
                    'N' => target != Entity.Null ? target.DisplayName : "someone",
                    'e' => Pronouns.Subject(actor.Get<Gender>().Value),
                    'E' => target != Entity.Null ? Pronouns.Subject(target.Get<Gender>().Value) : "they",
                    'm' => Pronouns.Object(actor.Get<Gender>().Value),
                    'M' => target != Entity.Null ? Pronouns.Object(target.Get<Gender>().Value) : "them",
                    's' => Pronouns.Possessive(actor.Get<Gender>().Value),
                    'S' => target != Entity.Null ? Pronouns.Possessive(target.Get<Gender>().Value) : "their",
                    _ => message[i].ToString()
                };

                foreach (char c in replacement)
                {
                    if (pos < buffer.Length)
                        buffer[pos++] = c;
                }
            }
            else
            {
                if (pos < buffer.Length)
                    buffer[pos++] = message[i];
            }
        }

        return new string(buffer.Slice(0, pos));
    }

    //// Core token replacement logic
    //private const int MaxMessageLength = 512;
    //public static void SendToRoom(
    //    Entity room,
    //    Entity actor,
    //    Entity target,
    //    string message)
    //{
    //    //Span<byte> buffer = stackalloc byte[MaxMessageLength];
    //    byte[] buffer = ArrayPool<byte>.Shared.Rent(MaxMessageLength);
    //    //byte[] buffer = new byte[512]; // stackalloc not allowed for async, so small array
    //    Encoding ascii = Encoding.ASCII;

    //    var roomContents = room.Get<RoomContents>();

    //    try
    //    {
    //        foreach (var player in roomContents.Characters)
    //        {
    //            int pos = 0;
    //            for (int i = 0; i < message.Length; i++)
    //            {
    //                if (message[i] == '$' && i + 1 < message.Length)
    //                {
    //                    i++;
    //                    ReadOnlySpan<byte> replacement;

    //                    switch (message[i])
    //                    {
    //                        case 'n':
    //                            replacement = (player == actor) ? ascii.GetBytes("you") : ascii.GetBytes(actor.Get<Name>().Value);
    //                            break;
    //                        case 'N':
    //                            replacement = (player == target) ? ascii.GetBytes("you") : ascii.GetBytes(target != Entity.Null ? target.Get<Name>().Value : "someone");
    //                            break;
    //                        case 'e':
    //                            replacement = (player == actor) ? ascii.GetBytes("you") : ascii.GetBytes(Pronouns.Subject(actor.Get<Gender>().Value));
    //                            break;
    //                        case 'E':
    //                            replacement = (player == target) ? ascii.GetBytes("you") : ascii.GetBytes(target != Entity.Null ? Pronouns.Subject(target.Get<Gender>().Value) : "they");
    //                            break;
    //                        case 'm':
    //                            replacement = (player == actor) ? ascii.GetBytes("you") : ascii.GetBytes(Pronouns.Object(actor.Get<Gender>().Value));
    //                            break;
    //                        case 'M':
    //                            replacement = (player == target) ? ascii.GetBytes("you") : ascii.GetBytes(target != Entity.Null ? Pronouns.Object(target.Get<Gender>().Value) : "them");
    //                            break;
    //                        case 's':
    //                            replacement = (player == actor) ? ascii.GetBytes("your") : ascii.GetBytes(Pronouns.Possessive(actor.Get<Gender>().Value));
    //                            break;
    //                        case 'S':
    //                            replacement = (player == target) ? ascii.GetBytes("your") : ascii.GetBytes(target != Entity.Null ? Pronouns.Possessive(target.Get<Gender>().Value) : "their");
    //                            break;
    //                        default:
    //                            replacement = ascii.GetBytes(new[] { message[i] });
    //                            break;
    //                    }

    //                    foreach (var b in replacement)
    //                    {
    //                        if (pos < buffer.Length)
    //                            buffer[pos++] = b;
    //                    }
    //                }
    //                else
    //                {
    //                    if (pos < buffer.Length)
    //                        buffer[pos++] = (byte)message[i];
    //                }
    //            }

    //            // Append newline
    //            if (pos < buffer.Length) buffer[pos++] = (byte)'\n';

    //            // Send bytes to the player's Telnet connection
    //            //MessageSystem.SendMessage(player, buffer.Slice(0, pos));
    //            MessageSystem.SendMessage(player, buffer.AsSpan(0, pos));
    //        }
    //    }
    //    finally
    //    {
    //        ArrayPool<byte>.Shared.Return(buffer);
    //    }
    //}

    public static class Pronouns
    {
        public static string Subject(GenderType g) => g switch
        {
            GenderType.Male => "he",
            GenderType.Female => "she",
            _ => "it"
        };

        public static string Object(GenderType g) => g switch
        {
            GenderType.Male => "him",
            GenderType.Female => "her",
            _ => "it"
        };

        public static string Possessive(GenderType g) => g switch
        {
            GenderType.Male => "his",
            GenderType.Female => "her",
            _ => "its"
        };
    }
}
