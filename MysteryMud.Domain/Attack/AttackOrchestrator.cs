using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Attack.Factories;
using MysteryMud.Domain.Attack.Resolvers;
using MysteryMud.Domain.Damage.Resolvers;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Attack;

public sealed class AttackOrchestrator
{
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AttackResolvedEvent> _attackResolved;
    private readonly DamageResolver _damageResolver;
    private readonly HitResolver _hitResolver;
    private readonly HitDamageFactory _hitDamageFactory;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;

    public AttackOrchestrator(IGameMessageService msg, IIntentContainer intents, IEventBuffer<AttackResolvedEvent> attackResolved, HitResolver hitResolver, HitDamageFactory hitDamageFactory, DamageResolver damageResolver, WeaponProcResolver weaponProcResolver, ReactionResolver reactionResolver)
    {
        _intents = intents;
        _attackResolved = attackResolved;
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
            switch (intent.Kind)
            {
                case AttackIntentKind.Hit:
                    ResolveAttack(state, ref intent);
                    break;
                case AttackIntentKind.Ability:
                    ResolveAbility(state, ref intent);
                    break;
                    // TODO
                //case OffensiveIntentKind.ChannelingAbility:
                //    ResolveChanneling(intent);
                //    break;
                //case AttackIntentKind.Reaction:
                //    ResolveReaction(ref intent);
                //    break;
                //case AttackIntentKind.Interrupt:
                //    ResolveInterrupt(ref intent);
                //    break;
            }
        }
    }

    private void ResolveAttack(GameState state, ref AttackIntent intent)
    {
        ref var attack = ref intent.Attack;

        if (!CharacterHelpers.IsAlive(attack.Attacker, attack.Target))
            return;

        var resolvedHit = _hitResolver.Resolve(attack);

        if (resolvedHit.Result == AttackResultKind.Hit)
        {
            var damageAction = _hitDamageFactory.CreateHitDamage(resolvedHit);

            _damageResolver.Resolve(state, damageAction);
        }

        if (!CharacterHelpers.IsAlive(attack.Target))
            return;

        _reactionResolver.Resolve(_intents, resolvedHit);

        // Multi-hit continuation
        if (!attack.IsReaction && resolvedHit.Result != AttackResultKind.Dodge && attack.RemainingHits > 1)
        {
            ref var nextMultiHitAttackIntent = ref _intents.Attack.Add();
            nextMultiHitAttackIntent.Attack.Attacker = attack.Attacker;
            nextMultiHitAttackIntent.Attack.Target = attack.Target;
            nextMultiHitAttackIntent.Attack.RemainingHits = attack.RemainingHits - 1;
            nextMultiHitAttackIntent.Attack.IsReaction = attack.IsReaction;
            nextMultiHitAttackIntent.Attack.IgnoreDefense = attack.IgnoreDefense;
        }
    }

    private void ResolveAbility(GameState state, ref AttackIntent intent)
    {
        ref var ability = ref intent.Ability;

        if (!CharacterHelpers.IsAlive(ability.Caster))
            return;

        // TODO
        //var def = GetAbility(ability.AbilityId);

        //foreach (var target in ability.Targets.Where(t => CharacterHelpers.IsAlive(t)))
        //{
        //    foreach (var eff in def.Effects)
        //    {
        //        _effectFactory.CreateEffect(GetEffect(eff.EffectId), ability.Caster, target, _state);
        //    }

        //    _reactionResolver.Resolve(_intents, new HitInfo
        //    {
        //        Source = ability.Caster,
        //        Target = target,
        //        Result = AttackResultKind.Hit
        //    });
        //}
    }

    //void ResolveChanneling(Intent intent)
    //    {
    //        var chan = intent.AsChanneling();
    
    //        if (!CharacterHelpers.IsAlive(chan.Caster))
    //            return;
    
    //        var abilityDef = GetAbility(chan.AbilityId);
    
    //        // Recompute targets each tick or use stored
    //        var targets = chan.Targets.Where(t => CharacterHelpers.IsAlive(t) && CharacterHelpers.IsAttackable(chan.Caster, t)).ToList();
    
    //        foreach (var target in targets)
    //        {
    //            foreach (var eff in abilityDef.Effects)
    //            {
    //                _effectFactory.CreateEffect(GetEffect(eff.EffectId), chan.Caster, target, _state);
    //            }
    
    //            _reactionResolver.Resolve(_nextQueue, new HitInfo
    //            {
    //                Source = chan.Caster,
    //                Target = target,
    //                Result = AttackResultKind.Hit
    //            });
    //        }
    
    //        chan.RemainingTicks--;
    
    //        if (chan.RemainingTicks > 0)
    //        {
    //            // Schedule the next tick for **next game tick**
    //            var nextTick = new Intent
    //            {
    //                Kind = IntentKind.ChannelingAbility,
    //                Channeling = chan
    //            };
    //            EnqueueNextTick(nextTick);
    //        }
    //    }

    //private void ResolveInterrupt(AttackIntent intent)
    //{
    //    ref var interrupt = ref intent.Interrupt;

    //    foreach (var pending in _nextQueue)
    //    {
    //        if (pending.Cancelled)
    //            continue;

    //        // Interrupt channeling or abilities
    //        if ((pending.Kind == IntentKind.ChannelingAbility && pending.AsChanneling().Caster == interrupt.Target) ||
    //            (pending.Kind == IntentKind.Ability && pending.AsAbility().Caster == interrupt.Target))
    //        {
    //            pending.Cancelled = true;
    //            _state.Log($"{interrupt.Target} was interrupted due to {interrupt.Reason}");
    //        }
    //    }
    //}
}



