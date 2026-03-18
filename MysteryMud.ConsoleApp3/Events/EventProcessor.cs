using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Events;

class EventProcessor
{
    public static void Execute(World world, ref TimedEvent ev)
    {
        switch (ev.Type)
        {
            case EventType.DotTick:
                DotSystem.HandleTick(world, ev.Target);
                break;

            case EventType.HotTick:
                HotSystem.HandleTick(world, ev.Target);
                break;

            case EventType.EffectExpired:
                DurationSystem.HandleExpiration(world, ev.Target);
                break;
        }
    }
}
