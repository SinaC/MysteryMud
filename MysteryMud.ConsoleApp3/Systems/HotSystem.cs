using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Events;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

static class HotSystem
{
    public static void HandleTick(World world, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<DeadTag>())
        {
            LogSystem.Log($"Applying HoT for Effect {effect.DisplayName} on DEAD Target");
            return;
        }

        ref var hot = ref effect.Get<HealOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (hot.NextTick >= duration.ExpirationTick)
        {
            LogSystem.Log($"Applying HoT damage for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName} with heal {hot.Heal} and tick rate {hot.TickRate} on EXPIRED effect");
            return;
        }

        LogSystem.Log($"Applying HoT damage for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName} with heal {hot.Heal} and tick rate {hot.TickRate}");

        // calcule next tick
        hot.NextTick = TimeSystem.CurrentTick + hot.TickRate;

        // perform damage
        var heal = hot.Heal * effectInstance.StackCount;
        HealSystem.ApplyHeal(world, effectInstance.Target, heal, effectInstance.Source);

        // queue next tick even if after expiration tick to handle effect refresh
        LogSystem.Log($"Scheduling next HoT tick for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName} at tick {hot.NextTick}");
        EventScheduler.Schedule(new TimedEvent
        {
            ExecuteAt = hot.NextTick,
            Target = effect,
            Type = EventType.HotTick
        });
    }
}
