using Arch.Core;

namespace MysteryMud.Infrastructure.Services;

public interface IOutputService
{
    void Send(Entity entity, string message); // batch messages for the same entity together to minimize network calls
    //void Flush(Entity entity); // optional, in case we want to batch messages and flush at the end of a tick
    void FlushAll(); // flush all pending messages to players
}
