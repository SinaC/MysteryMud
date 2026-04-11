using Arch.Core;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AbilityExecutionSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AbilityExecutedEvent> _abilityExecuted;
    private readonly AbilityRegistry _abilityRegistry;
    private readonly EffectRegistry _effectRegistry;
    private readonly AbilityExecutionResolverRegistry _abilityExecutionResolverRegistry;

    public AbilityExecutionSystem(ILogger logger, IGameMessageService msg, IIntentContainer intents, IEventBuffer<AbilityExecutedEvent> abilityExecuted, AbilityRegistry abilityRegistry, EffectRegistry effectRegistry, AbilityExecutionResolverRegistry abilityExecutionResolverRegistry)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _abilityExecuted = abilityExecuted;
        _abilityRegistry = abilityRegistry;
        _effectRegistry = effectRegistry;
        _abilityExecutionResolverRegistry = abilityExecutionResolverRegistry;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.ExecuteAbilitySpan)
        {
            var abilityId = intent.AbilityId;
            var source = intent.Source;
            var targets = intent.Targets;

            if (!_abilityRegistry.TryGetValue(abilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                continue;
            }

            // check if ability directly fails (skill learned % for example)
            if (abilityRuntime.Executor is { Hook: AbilityExecutorHook.Execution })
            {
                if (_abilityExecutionResolverRegistry.TryGetResolver(abilityRuntime.Executor.ExecutorId, out var registedResolver) && registedResolver is not null)
                {
                    var result = registedResolver.Resolver.Resolve(source, abilityRuntime);
                    SendAbilityMessage(source, abilityRuntime, result.Outcome);
                    if (!result.Success)
                        continue;
                }
            }

            // TODO: set cooldown

            foreach (var effectId in abilityRuntime.EffectIds)
            {
                if (!_effectRegistry.TryGetValue(effectId, out var effectRuntime) || effectRuntime == null)
                {
                    _logger.LogError("Ability {abilityName}: effect {effectId} not found", abilityRuntime.Name, effectId);
                    continue;
                }
                // add effect action for each target
                foreach (var target in targets)
                {
                    ref var effectIntent = ref _intents.Action.Add();
                    effectIntent.Kind = ActionKind.Effect;
                    effectIntent.Effect.EffectId = effectId;
                    effectIntent.Effect.Source = source;
                    effectIntent.Effect.Target = target;
                    effectIntent.Cancelled = false;
                }
            }

            // add ability execute event
            ref var abilityExecutedEvt = ref _abilityExecuted.Add();
            abilityExecutedEvt.AbilityId = abilityId;
            abilityExecutedEvt.Source = source;
            abilityExecutedEvt.Targets = targets;
        }
    }
    
    private void SendAbilityMessage(Entity actor, AbilityRuntime ability, string key) // TODO: same code found in AbilityExecutionSystem/AbilityCastingSystem
    {
        if (key is null)
            return;
        if (ability.Messages.TryGetValue(key, out var msg))
            _msg.To(actor).Send(msg);
        else
            _logger.LogError("Ability {abilityName} validation rules refers to {key} but it's not found in messages", ability.Name, key);
    }
}
