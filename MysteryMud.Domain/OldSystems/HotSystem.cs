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

public static class HotSystem
{
    public static void HandleTick(SystemContext ctx, GameState state, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<Dead>())
        {
            ctx.Log.LogInformation(LogEvents.Hot,"Ticking HoT for Effect {effectName} on DEAD Target {targetName}", effect.DebugName, effectInstance.Target.DebugName);
            return;
        }

        ref var hot = ref effect.Get<HealOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (hot.NextTick >= duration.ExpirationTick)
        {
            ctx.Log.LogInformation(LogEvents.Hot,"Ticking HoT for Effect {effectName} on Target {targetName} and tick rate {tickRate} on EXPIRED effect", effect.DebugName, effectInstance.Target.DebugName, hot.TickRate);
            return;
        }

        // perform heal
        var heal = hot.Heal * effectInstance.StackCount;
        ctx.Log.LogInformation(LogEvents.Hot,"Applying HoT heal for Effect {effectName} on Target {targetName} with heal {heal} and tick rate {tickRate}", effect.DebugName, effectInstance.Target.DebugName, heal, hot.TickRate);
        HealSystem.ApplyHeal(ctx, effectInstance.Target, heal, effectInstance.Source);

        // calcule next tick
        hot.NextTick = TimeSystem.CurrentTick + hot.TickRate;

        // queue next tick even if after expiration tick to handle effect refresh
        ctx.Log.LogInformation(LogEvents.Hot,"Scheduling next HoT tick for Effect {effectName} on Target {targetName} at tick {nextTick}", effect.DebugName, effectInstance.Target.DebugName, hot.NextTick);
        ctx.Scheduler.Schedule(effect, ScheduledEventType.HotTick, hot.NextTick);
    }
}
