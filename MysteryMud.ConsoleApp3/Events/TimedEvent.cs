using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Events;

struct TimedEvent
{
    public long ExecuteAt;
    public EventType Type;
    public Entity Target;
}
