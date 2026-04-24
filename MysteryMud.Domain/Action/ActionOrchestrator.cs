using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Action.Attack.Factories;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;
using TinyECS;

namespace MysteryMud.Domain.Action;

public class ActionOrchestrator
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AttackResolvedEvent> _attackResolved;
    private readonly IEventBuffer<EffectResolvedEvent> _effectResolved;
    private readonly IEventBuffer<AggressedEvent> _aggressed;
    private readonly IEventBuffer<KillRewardEvent> _killRewards;
    private readonly IEffectRegistry _effectRegistry;
    private readonly IEffectApplicationManager _effectApplicationManager;
    private readonly IExperienceService _experienceService;
    private readonly IDamageResolver _damageResolver;
    private readonly IHitResolver _hitResolver;
    private readonly IHitDamageFactory _hitDamageFactory;
    private readonly IWeaponProcResolver _weaponProcResolver;
    private readonly IReactionResolver _reactionResolver;

    public ActionOrchestrator(World world, ILogger logger, IIntentContainer intents, IEventBuffer<AttackResolvedEvent> attackResolved, IEventBuffer<EffectResolvedEvent> effectResolved, IEventBuffer<AggressedEvent> aggressed, IEventBuffer<KillRewardEvent> killRewards, IEffectRegistry effectRegistry, IEffectApplicationManager effectApplicationManager, IExperienceService experienceService, IHitResolver hitResolver, IHitDamageFactory hitDamageFactory, IDamageResolver damageResolver, IWeaponProcResolver weaponProcResolver, IReactionResolver reactionResolver)
    {
        _world = world;
        _logger = logger;
        _intents = intents;
        _attackResolved = attackResolved;
        _effectResolved = effectResolved;
        _aggressed = aggressed;
        _killRewards = killRewards;
        _effectRegistry = effectRegistry;
        _experienceService = experienceService;
        _effectApplicationManager = effectApplicationManager;
        _damageResolver = damageResolver;
        _hitResolver = hitResolver;
        _hitDamageFactory = hitDamageFactory;
        _weaponProcResolver = weaponProcResolver;
        _reactionResolver = reactionResolver;
    }

    public void Tick(GameState state)
    {
        for (int i = 0; i < _intents.ActionCount; i++) // we iterate using index to be able to add intents while iterating
        {
            var intent = _intents.ActionByIndex(i);
            if (intent.Cancelled)
                continue;

            // process intent
            switch (intent.Kind)
            {
                case ActionKind.Attack:
                    ResolveAttack(state, ref intent);
                    break;
                case ActionKind.Effect:
                    ResolveEffect(state, ref intent);
                    break;
            }

            // process immediate kill reward events to grant xp
            foreach (ref var killRewardEvt in _killRewards.GetAll())
            {
                GrantReward(ref killRewardEvt);
            }

            _killRewards.Clear(); // clear after processing
        }
    }

    private void ResolveAttack(GameState state, ref ActionIntent actionIntent)
    {
        ref var attackData = ref actionIntent.Attack;
        var attackSource = attackData.Source;
        var attackTarget = attackData.Source;

        if (!CharacterHelpers.IsAlive(_world, attackSource, attackTarget))
            return;

        // resolve hit
        var resolvedHit = _hitResolver.Resolve(attackData);

        // the intent was hostile regardless of hit/miss/dodge outcome
        ref var aggrEvt = ref _aggressed.Add();
        aggrEvt.Source = attackSource;
        aggrEvt.Target = attackTarget;

        // attack resolved event
        ref var attackResolvedEvt = ref _attackResolved.Add();
        attackResolvedEvt.Source = resolvedHit.Source;
        attackResolvedEvt.Target = resolvedHit.Target;
        attackResolvedEvt.Result = resolvedHit.Result;

        // if hit, resolve damage + weapon proc
        if (resolvedHit.Result == AttackResultKind.Hit)
        {
            // damage
            var damageAction = _hitDamageFactory.CreateHitDamage(resolvedHit);
            var damageResult = _damageResolver.Resolve(state, damageAction);
            // weapon proc
            _weaponProcResolver.Resolve(state, resolvedHit, damageResult);
        }

        if (!CharacterHelpers.IsAlive(_world, attackSource, attackTarget))
            return;

        // if still alive after damage, check reaction (such as counterattack)
        _reactionResolver.Resolve(_intents, resolvedHit);

        // multi-hit continuation
        if (!attackData.IsReaction && resolvedHit.Result != AttackResultKind.Dodge && attackData.RemainingHits > 1)
        {
            ref var nextMultiHitAttackIntent = ref _intents.Action.Add();
            nextMultiHitAttackIntent.Kind = ActionKind.Attack;
            nextMultiHitAttackIntent.Attack.Source = attackData.Source;
            nextMultiHitAttackIntent.Attack.Target = attackData.Target;
            nextMultiHitAttackIntent.Attack.RemainingHits = attackData.RemainingHits - 1;
            nextMultiHitAttackIntent.Attack.IsReaction = attackData.IsReaction;
            nextMultiHitAttackIntent.Attack.IgnoreDefense = attackData.IgnoreDefense;
        }
    }

    private void ResolveEffect(GameState state, ref ActionIntent actionIntent)
    {
        ref var effectData = ref actionIntent.Effect;
        var effectSource = effectData.Source;
        var effectTarget = effectData.Source;

        if (!CharacterHelpers.IsAlive(_world, effectSource, effectTarget))
            return;

        if (!_effectRegistry.TryGetRuntime(effectData.EffectId, out var effectRuntime) || effectRuntime == null)
        {
            _logger.LogError("Effect id {effectId} not found in the effect registry", effectData.EffectId);
            return;
        }
        _effectApplicationManager.CreateEffect(state, effectRuntime, ref effectData);

        // AGGRESSION: emit after CreateEffect, effect was attempted on a live target
        if (effectData.IsHarmful)
        {
            ref var aggrEvt = ref _aggressed.Add();
            aggrEvt.Source = effectSource;
            aggrEvt.Target = effectTarget;
        }

        // attack resolved event
        ref var effectResolvedEvt = ref _effectResolved.Add();
        effectResolvedEvt.Source = effectSource;
        effectResolvedEvt.Target = effectTarget;
        effectResolvedEvt.EffectId = effectData.EffectId;
    }

    private void GrantReward(ref KillRewardEvent killRewardEvt)
    {
        if (killRewardEvt.RewardOwner != killRewardEvt.Victim && _world.Has<Progression>(killRewardEvt.RewardOwner))
            GrantExperience(killRewardEvt.RewardOwner, killRewardEvt.Victim);

        if (killRewardEvt.RewardOwnerGroup != EntityId.Invalid)
        {
            ref var groupInstance = ref _world.TryGetRef<GroupInstance>(killRewardEvt.RewardOwnerGroup, out var isInGroup);
            if (isInGroup)
            {
                foreach (var member in groupInstance.Members)
                {
                    if (member == killRewardEvt.RewardOwner) continue;
                    if (!CharacterHelpers.IsAlive(_world, member)) continue; // <- member died too
                    if (!CharacterHelpers.SameRoom(_world, killRewardEvt.RewardOwner, member)) continue; // <- member not anymore in same room
                    GrantExperience(member, killRewardEvt.Victim);
                }
            }
        }
    }

    private void GrantExperience(EntityId target, EntityId victim)
    {
        var xpReward = _experienceService.CalculateCombatXp(target, victim);
        _experienceService.GrantExperience(target, xpReward);
    }
}