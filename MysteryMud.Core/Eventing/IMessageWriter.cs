using Arch.Core;

namespace MysteryMud.Core.Eventing;

public interface IMessageWriter
{
    void Send(Entity entity, string message);
    void Act(IEnumerable<Entity> entities, string format, params object[] arguments);

}
