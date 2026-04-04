using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Domain.Attack.Factories;
using MysteryMud.Domain.Attack.Resolvers;
using MysteryMud.Domain.Damage;
using MysteryMud.Domain.Effect.Factories;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Attack;

public sealed class AttackOrchestrator
{
    private readonly ILogger _logger;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AttackResolvedEvent> _attackResolved;
    private readonly EffectFactory _effectFactory;
    private readonly DamageResolver _damageResolver;
    private readonly HitResolver _hitResolver;
    private readonly HitDamageFactory _hitDamageFactory;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;

    public AttackOrchestrator(ILogger logger, IIntentContainer intents, IEventBuffer<AttackResolvedEvent> attackResolved, EffectFactory effectFactory, HitResolver hitResolver, HitDamageFactory hitDamageFactory, DamageResolver damageResolver, WeaponProcResolver weaponProcResolver, ReactionResolver reactionResolver)
    {
        _logger = logger;
        _intents = intents;
        _attackResolved = attackResolved;
        _effectFactory = effectFactory;
        _damageResolver = damageResolver;
        _hitResolver = hitResolver;
        _hitDamageFactory = hitDamageFactory;
        _weaponProcResolver = weaponProcResolver;
        _reactionResolver = reactionResolver;
    }

    public void Tick(GameState state)
    {
        // resolve one combat intent at a time to properly handle reactions between hits
        for (int i = 0; i < _intents.AttackCount; i++) // we iterate using index to be able to add intents while iterating
        {
            var intent = _intents.AttackByIndex(i);
            if (intent.Cancelled)
                continue;

            ResolveAttack(state, ref intent);
        }
    }

    private void ResolveAttack(GameState state, ref AttackIntent intent)
    {
        if (!CharacterHelpers.IsAlive(intent.Attacker, intent.Target))
            return;

        // resolve hit
        var resolvedHit = _hitResolver.Resolve(intent);

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

        if (!CharacterHelpers.IsAlive(intent.Target))
            return;

        // if still alive after damage, check reaction (such as counterattack)
        _reactionResolver.Resolve(_intents, resolvedHit);

        // multi-hit continuation
        if (!intent.IsReaction && resolvedHit.Result != AttackResultKind.Dodge && intent.RemainingHits > 1)
        {
            ref var nextMultiHitAttackIntent = ref _intents.Attack.Add();
            nextMultiHitAttackIntent.Attacker = intent.Attacker;
            nextMultiHitAttackIntent.Target = intent.Target;
            nextMultiHitAttackIntent.RemainingHits = intent.RemainingHits - 1;
            nextMultiHitAttackIntent.IsReaction = intent.IsReaction;
            nextMultiHitAttackIntent.IgnoreDefense = intent.IgnoreDefense;
        }
    }
}