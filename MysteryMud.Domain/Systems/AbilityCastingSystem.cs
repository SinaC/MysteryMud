using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Ability.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class AbilityCastingSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IAbilityTargetResolver _abilityTargetResolver;

    public AbilityCastingSystem(ILogger logger, IGameMessageService msg, IIntentContainer intents, IAbilityRegistry abilityRegistry, IAbilityTargetResolver abilityTargetResolver)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _abilityRegistry = abilityRegistry;
        _abilityTargetResolver = abilityTargetResolver;
    }

    public void Tick(GameState state)
    {
        // TODO: interrupt: Your concentration is broken! You fail to cast '{spellName}'.
        // TODO: use scheduler instead of looping each tick
        var query = new QueryDescription()
            .WithAll<Casting>()
            .WithNone<Dead>();
        state.World.Query(query, (Entity entity, ref Casting casting) =>
        {
            var abilityId = casting.AbilityId;
            var source = casting.Source;

            // casting time elapsed ?
            if (state.CurrentTick < casting.ExecuteAt)
            {
                if (state.CurrentTick - casting.LastUpdate >= 5 )
                {
                    _msg.To(source).Send(CastMessageHelpers.CasterTickMessage);
                    _msg.ToRoom(source).Act(CastMessageHelpers.RoomTickMessage).With(source);
                    casting.LastUpdate = state.CurrentTick;
                }
                return;
            }

            if (!_abilityRegistry.TryGetRuntime(casting.AbilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                return;
            }

            _msg.To(source).Act(CastMessageHelpers.CasterFinishMessage).With(abilityRuntime.Name);
            _msg.ToRoom(source).Act(CastMessageHelpers.RoomFinishMessage).With(source);

            List<Entity> targets;
            if (casting.ResolvedTargets is not null)
            {
                // Single-target or CastStart AoE: targets already locked in
                targets = casting.ResolvedTargets;
            }
            else
            {
                // AoE CastCompletion: resolve the room snapshot now
                var result = _abilityTargetResolver.Resolve(casting.Source, abilityRuntime.Targeting, state);
                var filtered = FilterTargets(abilityRuntime, result.Targets, casting.Source);
                targets = filtered;
            }

            // add execute ability intent
            ref var executeAbilityIntent = ref _intents.ExecuteAbility.Add();
            executeAbilityIntent.AbilityId = casting.AbilityId;
            executeAbilityIntent.Source = casting.Source;
            executeAbilityIntent.Targets = targets;

            // not casting anymore
            entity.Remove<Casting>();
        });
    }

    private List<Entity> FilterTargets(
            AbilityRuntime ability,
            List<Entity> candidates,
            Entity source)
    {
        var passed = new List<Entity>(candidates.Count);

        foreach (var target in candidates)
        {
            bool skip = false;

            foreach (var rule in ability.TargetValidationRules.Where(x => x.IsCandidateForValidation(target)))
            {
                var result = rule.Validate(target);
                if (result.Success)
                    continue;

                if (result.FailBehaviour == AbilityValidationFailBehaviour.SkipWithMessage
                    && result.FailMessageKey is not null)
                    ActAbilityMessage(source, ability, result.FailMessageKey, target);

                skip = true;
                break;
            }

            if (!skip)
                passed.Add(target);
        }

        return passed;
    }

    private void ActAbilityMessage(Entity actor, AbilityRuntime ability, string key, Entity target) // TODO: same code found in AbilityExecutionSystem
    {
        if (key is null)
            return;
        if (ability.Messages.TryGetValue(key, out var msg))
            _msg.To(actor).Act(msg).With(target);
        else
            _logger.LogError("Ability {abilityName} validation rules refers to {key} but it's not found in messages", ability.Name, key);
    }
}