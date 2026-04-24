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
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class AbilityCastingSystem
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly ICastMessageService _castMessageService;
    private readonly IIntentContainer _intents;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IAbilityTargetResolver _abilityTargetResolver;

    public AbilityCastingSystem(World world, ILogger logger, IGameMessageService msg, ICastMessageService castMessageService, IIntentContainer intents, IAbilityRegistry abilityRegistry, IAbilityTargetResolver abilityTargetResolver)
    {
        _world = world;
        _logger = logger;
        _msg = msg;
        _castMessageService = castMessageService;
        _intents = intents;
        _abilityRegistry = abilityRegistry;
        _abilityTargetResolver = abilityTargetResolver;
    }

    private static readonly QueryDescription _isCastingQueryDesc = new QueryDescription()
        .WithAll<Casting>()
        .WithNone<Dead>();

    public void Tick(GameState state)
    {
        // TODO: interrupt: Your concentration is broken! You fail to cast '{spellName}'.
        // TODO: use scheduler instead of looping each tick

        _world.Query(_isCastingQueryDesc, (EntityId entity,
            ref Casting casting) =>
        {
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

            List<EntityId> targets;
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
            _world.Remove<Casting>(entity);
        });
    }

    private List<EntityId> FilterTargets(
            AbilityRuntime ability,
            List<EntityId> candidates,
            EntityId source)
    {
        var passed = new List<EntityId>(candidates.Count);

        foreach (var target in candidates)
        {
            bool skip = false;

            foreach (var rule in ability.TargetValidationRules.Where(x => x.IsCandidateForValidation(_world, target)))
            {
                var result = rule.Validate(_world, source, target);
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

    private void ActAbilityMessage(EntityId actor, AbilityRuntime ability, string key, EntityId target) // TODO: same code found in AbilityValidationSystem
    {
        if (key is null)
            return;
        if (ability.Messages.TryGetValue(key, out var msg))
            ActAbilityMessage(msg, actor, target);
        else
            _logger.LogError("Ability {abilityName} validation rules refers to {key} but it's not found in messages", ability.Name, key);
    }

    private void ActAbilityMessage(ContextualizedMessage msg, EntityId actor, EntityId target) // TODO: same code found in EffectRuntimeFactory/AbilityValidationSystem
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

    private EntityId? GetMessageTarget(EntityId affectedEntity) // TODO: same code found in EffectRuntimeFactory/AbilityValidationSystem
    {
        if (_world.Has<CharacterTag>(affectedEntity))
            return affectedEntity;
        if (_world.TryGet<Equipped>(affectedEntity, out var equipped))
            return equipped.Wearer;
        if (_world.TryGet<ContainedIn>(affectedEntity, out var containedIn) && _world.Has<CharacterTag>(containedIn.Character))
            return containedIn.Character;
        return null;
    }
}