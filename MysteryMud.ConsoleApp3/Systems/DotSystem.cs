using Arch.Core;
using Arch.Core.Extensions;
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
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<DeadTag>())
        {
            LogSystem.Log($"Applying DoT for Effect {effect.DisplayName} on DEAD Target");
            return;
        }

        ref var dot = ref effect.Get<DamageOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (dot.NextTick >= duration.ExpirationTick)
        {
            LogSystem.Log($"Applying DoT damage for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName} with damage {dot.Damage} and tick rate {dot.TickRate} on EXPIRED effect");
            return;
        }

        LogSystem.Log($"Applying DoT damage for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName} with damage {dot.Damage} and tick rate {dot.TickRate}");

        // calcule next tick
        dot.NextTick = TimeSystem.CurrentTick + dot.TickRate;

        // perform damage
        var damage = dot.Damage * effectInstance.StackCount;
        DamageSystem.ApplyDamage(world, effectInstance.Target, damage, effectInstance.Source);

        // queue next tick even if after expiration tick to handle effect refresh
        LogSystem.Log($"Scheduling next DoT tick for Effect {effect.DisplayName} on Target {effectInstance.Target.DisplayName} at tick {dot.NextTick}");
        EventScheduler.Schedule(new TimedEvent
        {
            ExecuteAt = dot.NextTick,
            Target = effect,
            Type = EventType.DotTick
        });
    }
}
