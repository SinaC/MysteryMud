using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Domain.Attack.Factories;
using MysteryMud.Domain.Attack.Resolvers;
using MysteryMud.Domain.Damage.Resolvers;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Attack;

public sealed class AttackOrchestrator
{
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AttackResolvedEvent> _attackResolved;
    private readonly SpellDatabase _spellDatabase;
    private readonly EffectFactory _effectFactory;
    private readonly DamageResolver _damageResolver;
    private readonly HitResolver _hitResolver;
    private readonly HitDamageFactory _hitDamageFactory;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;

    public AttackOrchestrator(IIntentContainer intents, IEventBuffer<AttackResolvedEvent> attackResolved, SpellDatabase spellDatabase, EffectFactory effectFactory, HitResolver hitResolver, HitDamageFactory hitDamageFactory, DamageResolver damageResolver, WeaponProcResolver weaponProcResolver, ReactionResolver reactionResolver)
    {
        _intents = intents;
        _attackResolved = attackResolved;
        _spellDatabase = spellDatabase;
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
            switch (intent.Kind)
            {
                case AttackKind.Hit:
                    ResolveAttack(state, ref intent);
                    break;
                case AttackKind.Ability:
                    ResolveAbility(state, ref intent);
                    break;
            }
        }
    }

    private void ResolveAttack(GameState state, ref AttackIntent intent)
    {
        ref var attack = ref intent.Hit;

        if (!CharacterHelpers.IsAlive(attack.Attacker, attack.Target))
            return;

        // resolve hit
        var resolvedHit = _hitResolver.Resolve(attack);

        // attack resolved event
        ref var attackResolvedEvt = ref _attackResolved.Add();
        attackResolvedEvt.Source = resolvedHit.Source;
        attackResolvedEvt.Target = resolvedHit.Target;
        attackResolvedEvt.Result = resolvedHit.Result;

        // if hit, resolve damage
        if (resolvedHit.Result == AttackResultKind.Hit)
        {
            var damageAction = _hitDamageFactory.CreateHitDamage(resolvedHit);

            _damageResolver.Resolve(state, damageAction);
        }

        if (!CharacterHelpers.IsAlive(attack.Target))
            return;

        // if still alive after damage, check reaction (such as counterattack)
        _reactionResolver.Resolve(_intents, resolvedHit);

        // multi-hit continuation
        if (!attack.IsReaction && resolvedHit.Result != AttackResultKind.Dodge && attack.RemainingHits > 1)
        {
            ref var nextMultiHitAttackIntent = ref _intents.Attack.Add();
            nextMultiHitAttackIntent.Hit.Attacker = attack.Attacker;
            nextMultiHitAttackIntent.Hit.Target = attack.Target;
            nextMultiHitAttackIntent.Hit.RemainingHits = attack.RemainingHits - 1;
            nextMultiHitAttackIntent.Hit.IsReaction = attack.IsReaction;
            nextMultiHitAttackIntent.Hit.IgnoreDefense = attack.IgnoreDefense;
        }
    }

    private void ResolveAbility(GameState state, ref AttackIntent intent)
    {
        ref var ability = ref intent.Ability;

        if (!CharacterHelpers.IsAlive(ability.Caster))
            return;

        if (!_spellDatabase.Spells.TryGetValue(ability.AbilityId, out var def))
            return; // spell not found

        foreach (var target in ability.Targets.Where(t => CharacterHelpers.IsAlive(t)))
        {
            foreach (var eff in def.Effects) // TODO: saves vs spell
            {
                _effectFactory.ApplyEffect(state, eff, ability.Caster, target); // TODO: effect intent ?
            }

            // attack resolved event
            ref var attackResolvedEvt = ref _attackResolved.Add();
            attackResolvedEvt.Source = ability.Caster;
            attackResolvedEvt.Target = target;
            attackResolvedEvt.Result = AttackResultKind.Hit; // TODO

            // TODO
            //_reactionResolver.Resolve(_intents, new HitInfo
            //{
            //    Source = ability.Caster,
            //    Target = target,
            //    Result = AttackResultKind.Hit
            //});
        }
    }
}