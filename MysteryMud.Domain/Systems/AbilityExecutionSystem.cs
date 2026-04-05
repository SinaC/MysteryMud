using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Combat.Effect;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AbilityExecutionSystem
{
    private readonly ILogger _logger;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AbilityExecutedEvent> _abilityExecuted;
    private readonly AbilityRegistry _abilityRegistry;
    private readonly EffectRegistry _effectRegistry;

    public AbilityExecutionSystem(ILogger logger, IIntentContainer intents, IEventBuffer<AbilityExecutedEvent> abilityExecuted, AbilityRegistry abilityRegistry, EffectRegistry effectRegistry)
    {
        _logger = logger;
        _intents = intents;
        _abilityExecuted = abilityExecuted;
        _abilityRegistry = abilityRegistry;
        _effectRegistry = effectRegistry;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.ExecuteAbilitySpan)
        {
            if (!_abilityRegistry.TryGetValue(intent.AbilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", intent.AbilityId);
                continue;
            }

            // TODO: check targets by effect
            // TODO: set cooldown

            foreach (var effectId in abilityRuntime.EffectIds)
            {
                if (!_effectRegistry.TryGetValue(effectId, out var effectRuntime) || effectRuntime == null)
                {
                    _logger.LogError("Ability {abilityName}: effect {effectId} not found", abilityRuntime.Name, effectId);
                    continue;
                }

                // add effect action
                ref var effectIntent = ref _intents.Action.Add();
                effectIntent.Kind = ActionKind.Effect;
                effectIntent.Effect.EffectId = effectId;
                effectIntent.Effect.Source = intent.Source;
                effectIntent.Effect.Target = intent.Targets[0]; // TODO:
            }

            // add ability execute event
            ref var abilityExecutedEvt = ref _abilityExecuted.Add();
            abilityExecutedEvt.AbilityId = intent.AbilityId;
            abilityExecutedEvt.Source = intent.Source;
            abilityExecutedEvt.Targets = intent.Targets;
        }
    }
}
