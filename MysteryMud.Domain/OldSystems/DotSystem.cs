using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;

namespace MysteryMud.Domain.OldSystems;

public static class DotSystem
{
    public static void HandleTick(SystemContext ctx, GameState state, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<Dead>())
        {
            ctx.Log.LogInformation(LogEvents.Dot,"Ticking DoT for Effect {effectName} on DEAD Target {targetName}", effect.DebugName, effectInstance.Target.DebugName);
            return;
        }

        ref var dot = ref effect.Get<DamageOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (dot.NextTick >= duration.ExpirationTick)
        {
            ctx.Log.LogInformation(LogEvents.Dot,"Ticking DoT for Effect {effectName} on Target {targetName} and tick rate {tickRate} on EXPIRED effect", effect.DebugName, effectInstance.Target.DebugName, dot.TickRate);
            return;
        }

        // perform damage
        var damage = dot.Damage * effectInstance.StackCount;
        ctx.Log.LogInformation(LogEvents.Dot,"Applying DoT damage for Effect {effectName} on Target {targetName} with damage {damage} type {damageType} and tick rate {tickRate}", effect.DebugName, effectInstance.Target.DebugName, damage, dot.DamageType, dot.TickRate);
        DamageSystem.ApplyDamage(ctx, effectInstance.Target, damage, dot.DamageType, effectInstance.Source);

        // killed ?
        if (effectInstance.Target.Has<Dead>())
        {
            ctx.Log.LogInformation(LogEvents.Dot,"Target {targetName} died from DoT damage of Effect {effectName}", effectInstance.Target.DebugName, effect.DebugName);
            return;
        }

        // calcule next tick
        dot.NextTick = TimeSystem.CurrentTick + dot.TickRate;

        // queue next tick even if after expiration tick to handle effect refresh
        ctx.Log.LogInformation(LogEvents.Dot,"Scheduling next DoT tick for Effect {effectName} on Target {targetName} at tick {nextTick}", effect.DebugName, effectInstance.Target.DebugName, dot.NextTick);
        ctx.Scheduler.Schedule(effect, ScheduledEventType.DotTick, dot.NextTick);
    }
}
