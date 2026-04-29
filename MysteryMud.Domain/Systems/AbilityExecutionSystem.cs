using DefaultEcs;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AbilityExecutionSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AbilityExecutedEvent> _abilityExecuted;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IEffectRegistry _effectRegistry;
    private readonly IAbilityOutcomeResolverRegistry _abilityOutcomeResolverRegistry;

    public AbilityExecutionSystem(ILogger logger, IGameMessageService msg, IIntentContainer intents, IEventBuffer<AbilityExecutedEvent> abilityExecuted, IAbilityRegistry abilityRegistry, IEffectRegistry effectRegistry, IAbilityOutcomeResolverRegistry abilityOutcomeResolverRegistry)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _abilityExecuted = abilityExecuted;
        _abilityRegistry = abilityRegistry;
        _effectRegistry = effectRegistry;
        _abilityOutcomeResolverRegistry = abilityOutcomeResolverRegistry;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.ExecuteAbilitySpan)
        {
            var abilityId = intent.AbilityId;
            var source = intent.Source;
            var targets = intent.Targets;

            if (!_abilityRegistry.TryGetRuntime(abilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                continue;
            }

            // check if ability directly fails (skill learned % for example)
            if (abilityRuntime.OutcomeResolver is { Hook: AbilityOutcomeHook.Execution })
            {
                if (_abilityOutcomeResolverRegistry.TryGetResolver(abilityRuntime.OutcomeResolver.ResolverId, out var registedResolver) && registedResolver is not null)
                {
                    var result = registedResolver.Resolver.Resolve(source, abilityRuntime);
                    SendAbilityOutcomeMessage(source, abilityRuntime, result.Outcome);
                    if (!result.Success)
                        continue;
                }
            }

            // TODO: set cooldown

            // if an ability has conditional effects:
            //  IsWeapon: effect a
            //  IsItem: effect b
            // when using on a weapon, effect a+b will be applied
            // we only want a to be applied
            foreach (var target in targets)
            {
                // TODO: check if still in same room ? except for some effect such as summon
                if (!CharacterHelpers.IsAlive(target))
                    continue;

                // Find the first (most specific) condition group that matches this target
                var matchedGroup = abilityRuntime.ConditionalEffectGroups
                    .OrderByDescending(g => g.Condition.Specificity())
                    .FirstOrDefault(g => g.Condition.Matches(target));

                if (matchedGroup is null)
                    continue;

                foreach (var effectId in matchedGroup.EffectIds)
                {
                    if (!_effectRegistry.TryGetRuntime(effectId, out var effectRuntime) || effectRuntime == null)
                    {
                        _logger.LogError("Ability {abilityName}: effect {effectId} not found",
                            abilityRuntime.Name, effectId);
                        continue;
                    }

                    ref var effectIntent = ref _intents.Action.Add();
                    effectIntent.Kind = ActionKind.Effect;
                    effectIntent.Effect.EffectId = effectId;
                    effectIntent.Effect.Source = source;
                    effectIntent.Effect.Target = target;
                    effectIntent.Effect.IsHarmful = effectRuntime.IsHarmful;
                    effectIntent.Effect.EffectiveDamageAmount = 0;
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

    private void SendAbilityOutcomeMessage(Entity actor, AbilityRuntime ability, string key)
    {
        if (key is null)
            return;
        if (ability.Messages.TryGetValue(key, out var msg) && msg.ToActor is not null) // outcome messages are only for actor
            _msg.To(actor).Send(msg.ToActor);
        else
            _logger.LogError("Ability {abilityName} validation rules refers to {key} but it's not found in messages", ability.Name, key);
    }
}
