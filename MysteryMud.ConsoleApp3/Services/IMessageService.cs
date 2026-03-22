using Arch.Core;

namespace MysteryMud.ConsoleApp3.Services;

public interface IMessageService
{
    void Send(Entity entity, string message); // batch messages for the same entity together to minimize network calls
    void Flush(Entity entity); // optional, in case we want to batch messages and flush at the end of a tick
}
