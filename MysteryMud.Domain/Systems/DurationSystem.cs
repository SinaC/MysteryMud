using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Factories;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class DurationSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IEventBuffer<EffectExpiredEvent> _expired;

    public DurationSystem(ILogger logger, IGameMessageService msg, IEventBuffer<EffectExpiredEvent> expired)
    {
        _logger = logger;
        _msg = msg;
        _expired = expired;
    }

    public void Tick(GameState state)
    {
        foreach (ref var expired in _expired.GetAll())
            ProcessOneEffect(state, expired.Effect);
    }

    public void ProcessOneEffect(GameState state, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive())
        {
            // target is already dead, just clean up the effect
            state.World.Destroy(effect);
            return;
        }

        ref var duration = ref effect.Get<Duration>();

        if (duration.ExpirationTick != state.CurrentTick)
        {
            _logger.LogInformation(LogEvents.Duration,"Rescheduled Duration for Effect {effectName} on Target {targetName} with Expiration Tick {expirationTick} but Current Tick {currentTick}", effect.DebugName, effectInstance.Target.DebugName, duration.ExpirationTick, state.CurrentTick);
            return;
        }

        _logger.LogInformation(LogEvents.Duration,"Expiring Duration for Effect {effectName} on Target {targetName}", effect.DebugName, effectInstance.Target.DebugName);

        // remove the effect from the target's CharacterEffects
        EffectFactory.RemoveEffect(state, effect);

        // display wear off message if any
        if (effectInstance.Template.WearOffMessage != null)
        {
            // TODO: in room ?
            _msg.To(effectInstance.Target).Send(effectInstance.Template.WearOffMessage);
        }
    }
}
