using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
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
                    SendAbilityMessage(source, abilityRuntime, result.Outcome);
                    if (!result.Success)
                        continue;
                }
            }

            // TODO: set cooldown

            foreach (var conditionalEffectGroup in abilityRuntime.ConditionalEffectGroups)
            {
                var resolvedTargets = ResolveConditionalTargets(targets, conditionalEffectGroup.Condition);
                if (resolvedTargets.Count == 0)
                    continue;
                foreach (var effectId in conditionalEffectGroup.EffectIds)
                {
                    if (!_effectRegistry.TryGetRuntime(effectId, out var effectRuntime) || effectRuntime == null)
                    {
                        _logger.LogError("Ability {abilityName}: effect {effectId} not found", abilityRuntime.Name, effectId);
                        continue;
                    }
                    // add effect action for each target
                    foreach (var target in resolvedTargets)
                    {
                        ref var effectIntent = ref _intents.Action.Add();
                        effectIntent.Kind = ActionKind.Effect;
                        effectIntent.Effect.EffectId = effectId;
                        effectIntent.Effect.Source = source;
                        effectIntent.Effect.Target = target;
                        effectIntent.Cancelled = false;
                    }
                }
            }

            // add ability execute event
            ref var abilityExecutedEvt = ref _abilityExecuted.Add();
            abilityExecutedEvt.AbilityId = abilityId;
            abilityExecutedEvt.Source = source;
            abilityExecutedEvt.Targets = targets;
        }
    }

    private List<Entity> ResolveConditionalTargets(List<Entity> targets, AbilityEffectCondition condition)
    {
        // TODO: change condition to a string and use a condition registry (with interface similar to OutcomeResolver)
        switch (condition)
        {
            case AbilityEffectCondition.IsCharacter: return targets.Where(x => x.Has<CharacterTag>()).ToList();
            case AbilityEffectCondition.IsItem: return targets.Where(x => x.Has<ItemTag>()).ToList();
            case AbilityEffectCondition.IsNPC: return targets.Where(x => x.Has<NpcTag>()).ToList();
            case AbilityEffectCondition.IsPlayer: return targets.Where(x => x.Has<PlayerTag>()).ToList();
            case AbilityEffectCondition.IsWeapon: return targets.Where(x => x.Has<Weapon>()).ToList();
        }
        return targets;
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
