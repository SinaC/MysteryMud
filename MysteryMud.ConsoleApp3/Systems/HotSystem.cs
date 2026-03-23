using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Simulation.Calculators;

namespace MysteryMud.ConsoleApp3.Systems;

static class HotSystem
{
    public static void HandleTick(GameState state, Entity effect)
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

        // perform damage
        var heal = hot.Heal * effectInstance.StackCount;
        Logger.Logger.Hot.ApplyHeal(effect, effectInstance.Target, heal, hot.TickRate);
        HealCalculator.ApplyHeal(effectInstance.Target, heal, effectInstance.Source);

        // calcule next tick
        hot.NextTick = TimeSystem.CurrentTick + hot.TickRate;

        // queue next tick even if after expiration tick to handle effect refresh
        Logger.Logger.Hot.ScheduleNextTick(effect, effectInstance.Target, hot.NextTick);
        Scheduler.Publish(new ScheduledEvent
        {
            ExecuteAt = hot.NextTick,
            Target = effect,
            Type = ScheduledEventType.HotTick
        });
    }
}
