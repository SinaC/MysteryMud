using DefaultEcs;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Ability.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class AbilityCastingSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly ICastMessageService _castMessageService;
    private readonly IIntentContainer _intents;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IAbilityTargetResolver _abilityTargetResolver;
    private readonly EntitySet _castingEntitySet;

    public AbilityCastingSystem(World world, ILogger logger, IGameMessageService msg, ICastMessageService castMessageService, IIntentContainer intents, IAbilityRegistry abilityRegistry, IAbilityTargetResolver abilityTargetResolver)
    {
        _logger = logger;
        _msg = msg;
        _castMessageService = castMessageService;
        _intents = intents;
        _abilityRegistry = abilityRegistry;
        _abilityTargetResolver = abilityTargetResolver;
        _castingEntitySet = world
            .GetEntities()
            .With<Casting>()
            .Without<DeadTag>()
            .AsSet();
    }

    public void Tick(GameState state)
    {
        // TODO: interrupt: Your concentration is broken! You fail to cast '{spellName}'.
        // TODO: use scheduler instead of looping each tick
        foreach(var entity in _castingEntitySet.GetEntities())
        {
            ref var casting = ref entity.Get<Casting>();
            var abilityId = casting.AbilityId;
            var source = casting.Source;

            // casting time elapsed ?
            if (state.CurrentTick < casting.ExecuteAt)
            {
                if (state.CurrentTick - casting.LastUpdate >= 5 )
                {
                    _msg.To(source).Send(_castMessageService.CasterTickMessage);
                    _msg.ToRoom(source).Act(_castMessageService.RoomTickMessage).With(source);
                    casting.LastUpdate = state.CurrentTick;
                }
                return;
            }

            if (!_abilityRegistry.TryGetRuntime(casting.AbilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                return;
            }

            _msg.To(source).Act(_castMessageService.CasterFinishMessage).With(abilityRuntime.Name);
            _msg.ToRoom(source).Act(_castMessageService.RoomFinishMessage).With(source);

            List<Entity> targets;
            if (casting.ResolvedTargets is not null)
            {
                // Single-target or CastStart AoE: targets already locked in
                targets = casting.ResolvedTargets;
            }
            else
            {
                // AoE CastCompletion: resolve the room snapshot now
                var result = _abilityTargetResolver.Resolve(casting.Source, abilityRuntime.Targeting);
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
        }
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
                var result = rule.Validate(source, target);
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

    private void ActAbilityMessage(Entity actor, AbilityRuntime ability, string key, Entity target) // TODO: same code found in AbilityValidationSystem
    {
        if (key is null)
            return;
        if (ability.Messages.TryGetValue(key, out var msg))
            ActAbilityMessage(msg, actor, target);
        else
            _logger.LogError("Ability {abilityName} validation rules refers to {key} but it's not found in messages", ability.Name, key);
    }

    private void ActAbilityMessage(ContextualizedMessage msg, Entity actor, Entity target) // TODO: same code found in EffectRuntimeFactory/AbilityValidationSystem
    {
        var source = actor;
        var affectedEntity = target;
        if (msg.ToActor is not null)
            _msg.To(source).Act(msg.ToActor).With(affectedEntity);
        if (msg.ToTarget is not null)
        {
            var messageTarget = GetMessageTarget(affectedEntity);
            if (messageTarget is not null)
                _msg.To(messageTarget.Value).Send(msg.ToTarget);
        }
        if (msg.ToRoom is not null)
        {
            var messageTarget = GetMessageTarget(affectedEntity);
            if (messageTarget is not null)
                _msg.ToRoomExcept(source, messageTarget.Value).Act(msg.ToRoom).With(affectedEntity);
            else
                _msg.ToRoom(source).Act(msg.ToRoom).With(affectedEntity);
        }
    }

    private Entity? GetMessageTarget(Entity affectedEntity) // TODO: same code found in EffectRuntimeFactory/AbilityValidationSystem
    {
        if (affectedEntity.Has<CharacterTag>())
            return affectedEntity;
        if (affectedEntity.Has<Equipped>())
            return affectedEntity.Get<Equipped>().Wearer;
        if (affectedEntity.Has<ContainedIn>())
        {
            ref var containedIn = ref affectedEntity.Get<ContainedIn>();
            if (containedIn.Character.Has<CharacterTag>())
                return containedIn.Character;
        }
        return null;
    }
}