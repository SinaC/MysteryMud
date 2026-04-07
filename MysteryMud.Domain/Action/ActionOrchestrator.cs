using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Domain.Action.Attack.Factories;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Factories;
using MysteryMud.Domain.Helpers;
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
    private readonly EffectRegistry _effectRegistry;
    private readonly EffectFactory _effectFactory;
    private readonly DamageResolver _damageResolver;
    private readonly HitResolver _hitResolver;
    private readonly HitDamageFactory _hitDamageFactory;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;

    public ActionOrchestrator(ILogger logger, IIntentContainer intents, IEventBuffer<AttackResolvedEvent> attackResolved, IEventBuffer<EffectResolvedEvent> effectResolved, EffectRegistry effectRegistry, EffectFactory effectFactory, HitResolver hitResolver, HitDamageFactory hitDamageFactory, DamageResolver damageResolver, WeaponProcResolver weaponProcResolver, ReactionResolver reactionResolver)
    {
        _logger = logger;
        _intents = intents;
        _attackResolved = attackResolved;
        _effectResolved = effectResolved;
        _effectRegistry = effectRegistry;
        _effectFactory = effectFactory;
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

            switch (intent.Kind)
            {
                case ActionKind.Attack:
                    ResolveAttack(state, ref intent);
                    break;
                case ActionKind.Effect:
                    ResolveEffect(state, ref intent);
                    break;
            }
        }
    }

    private void ResolveAttack(GameState state, ref ActionIntent actionIntent)
    {
        ref var attackData = ref actionIntent.Attack;

        if (!CharacterHelpers.IsAlive(attackData.Source, attackData.Target))
            return;

        // resolve hit
        var resolvedHit = _hitResolver.Resolve(attackData);

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
            _damageResolver.Resolve(state, damageAction);
            // weapon proc
            _weaponProcResolver.Resolve(state, resolvedHit);
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

        if (!_effectRegistry.TryGetValue(effectData.EffectId, out var effectRuntime) || effectRuntime == null)
        {
            _logger.LogError("Effect id {effectId} not found in the effect registry", effectData.EffectId);
            return;
        }
        _effectFactory.ResolveEffect(state, effectRuntime, effectData.Source, effectData.Target);

        // attack resolved event
        ref var effectResolvedEvt = ref _effectResolved.Add();
        effectResolvedEvt.Source = effectData.Source;
        effectResolvedEvt.Target = effectData.Target;
        effectResolvedEvt.EffectId = effectData.EffectId;
    }
}
