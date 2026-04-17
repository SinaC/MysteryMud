using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Action.Attack.Factories;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Action;

public class ActionOrchestrator
{
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

    public ActionOrchestrator(ILogger logger, IIntentContainer intents, IEventBuffer<AttackResolvedEvent> attackResolved, IEventBuffer<EffectResolvedEvent> effectResolved, IEventBuffer<AggressedEvent> aggressed, IEventBuffer<KillRewardEvent> killRewards, IEffectRegistry effectRegistry, IEffectApplicationManager effectApplicationManager, IExperienceService experienceService, IHitResolver hitResolver, IHitDamageFactory hitDamageFactory, IDamageResolver damageResolver, IWeaponProcResolver weaponProcResolver, IReactionResolver reactionResolver)
    {
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

            // process immediate death events to grant xp
            foreach (var killRewardEvt in _killRewards.GetAll())
            {
                if (killRewardEvt.Killer != killRewardEvt.Victim && killRewardEvt.Killer.Has<Progression>())
                {
                    var xpReward = _experienceService.CalculateCombatXp(killRewardEvt.Killer, killRewardEvt.Victim);
                    _experienceService.GrantExperience(killRewardEvt.Killer, xpReward);
                }
            }

            _killRewards.Clear(); // clear after processing
        }
    }

    private void ResolveAttack(GameState state, ref ActionIntent actionIntent)
    {
        ref var attackData = ref actionIntent.Attack;

        if (!CharacterHelpers.IsAlive(attackData.Source, attackData.Target))
            return;

        // resolve hit
        var resolvedHit = _hitResolver.Resolve(attackData);

        // the intent was hostile regardless of hit/miss/dodge outcome
        ref var aggrEvt = ref _aggressed.Add();
        aggrEvt.Source = attackData.Source;
        aggrEvt.Target = attackData.Target;

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

        if (!CharacterHelpers.IsAlive(attackData.Target))
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

        if (!CharacterHelpers.IsAlive(effectData.Source, effectData.Target))
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
            aggrEvt.Source = effectData.Source;
            aggrEvt.Target = effectData.Target;
        }

        // attack resolved event
        ref var effectResolvedEvt = ref _effectResolved.Add();
        effectResolvedEvt.Source = effectData.Source;
        effectResolvedEvt.Target = effectData.Target;
        effectResolvedEvt.EffectId = effectData.EffectId;
    }
}
