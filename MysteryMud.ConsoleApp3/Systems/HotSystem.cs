using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Effects;

namespace MysteryMud.ConsoleApp3.Systems;

static class HotSystem
{
    public static void HandleTick(SystemContext systemContext, GameState state, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<Dead>())
        {
            Logger.Logger.Hot.TickOnDeadTarget(effect, effectInstance.Target);
            return;
        }

        ref var hot = ref effect.Get<HealOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (hot.NextTick >= duration.ExpirationTick)
        {
            Logger.Logger.Hot.TickAfterExpirationTime(effect, effectInstance.Target, hot.TickRate);
            return;
        }

        // perform heal
        var heal = hot.Heal * effectInstance.StackCount;
        Logger.Logger.Hot.ApplyHeal(effect, effectInstance.Target, heal, hot.TickRate);
        HealSystem.ApplyHeal(systemContext, effectInstance.Target, heal, effectInstance.Source);

        // calcule next tick
        hot.NextTick = TimeSystem.CurrentTick + hot.TickRate;

        // queue next tick even if after expiration tick to handle effect refresh
        Logger.Logger.Hot.ScheduleNextTick(effect, effectInstance.Target, hot.NextTick);
        systemContext.Scheduler.Publish(effect, ScheduledEventType.HotTick, hot.NextTick);
    }
}
