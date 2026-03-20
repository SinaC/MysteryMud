using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Calculators;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Events;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

static class DotSystem
{
    public static void HandleTick(World world, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<Dead>())
        {
            Logger.Logger.Dot.TickOnDeadTarget(effect, effectInstance.Target);
            return;
        }

        ref var dot = ref effect.Get<DamageOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (dot.NextTick >= duration.ExpirationTick)
        {
            Logger.Logger.Dot.TickAfterExpirationTime(effect, effectInstance.Target, dot.TickRate);
            return;
        }

        // perform damage
        var damage = dot.Damage * effectInstance.StackCount;
        Logger.Logger.Dot.ApplyDamage(effect, effectInstance.Target, damage, dot.DamageType, dot.TickRate);
        DamageCalculator.ApplyDamage(effectInstance.Target, damage, dot.DamageType, effectInstance.Source);

        // killed ?
        if (effectInstance.Target.Has<Dead>())
        {
            Logger.Logger.Dot.TargetKilled(effect, effectInstance.Target);
            return;
        }

        // calcule next tick
        dot.NextTick = TimeSystem.CurrentTick + dot.TickRate;

        // queue next tick even if after expiration tick to handle effect refresh
        Logger.Logger.Dot.ScheduleNextTick(effect, effectInstance.Target, dot.NextTick);
        EventScheduler.Schedule(new TimedEvent
        {
            ExecuteAt = dot.NextTick,
            Target = effect,
            Type = EventType.DotTick
        });
    }
}
