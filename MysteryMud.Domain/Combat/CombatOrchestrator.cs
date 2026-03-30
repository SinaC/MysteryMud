using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Combat.Resolvers;
using MysteryMud.Domain.Damage.Factories;
using MysteryMud.Domain.Damage.Resolvers;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat;

public sealed class CombatOrchestrator
{
    private readonly IIntentContainer _intents;
    private readonly DamageResolver _damageResolver;
    private readonly HitResolver _hitResolver;
    private readonly DamageFactory _damageProducer;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;

    public CombatOrchestrator(IGameMessageService msg, IIntentContainer intents, DamageResolver damageResolver)
    {
        _intents = intents;
        _damageResolver = damageResolver;
        _hitResolver = new HitResolver(msg); // TODO: don't create instance, inject it instead
        _damageProducer = new DamageFactory(); // TODO: don't create instance, inject it instead
        _weaponProcResolver = new WeaponProcResolver(); // TODO: don't create instance, inject it instead
        _reactionResolver = new ReactionResolver(msg); // TODO: don't create instance, inject it instead
    }

    // TODO: weapon proc can generate damage/heal/poison effect/...
    public void Tick(GameState state)
    {
        // resolve one combat intent at a time to properly handle reactions between hits
        for(int i = 0; i < _intents.AttackCount; i++) // we iterate using index to be able to add intents while iterating
        {
            var intent = _intents.AttackByIndex(i);
            if (!CharacterHelpers.IsAlive(intent.Attacker, intent.Target))
                continue;

            // ---------- Phase 1: Resolve Hit ----------
            var resolvedHit = _hitResolver.Resolve(intent);

            // ---------- Phase 2: Produce Damage ----------
            // if hit is successful, produce damage and handle reactions before resolving next hit, this way we can properly handle counterattacks between hits instead of waiting for all hits to be resolved and then handling reactions which can lead to unrealistic scenarios like player attacking 3 times and then monster counterattacking 3 times even if the player is already dead after the first hit
            if (resolvedHit.Result == AttackResultKind.Hit)
            {
                var damageAction = _damageProducer.CreateHitDamage(resolvedHit);

                _damageResolver.Resolve(damageAction);

                // ---------- Weapon procs (immediate on hit) ----------
                // TODO: handle weapon proc
            }

            // stop iteration if target is dead, no need to resolve reactions or continue multi-hit if target is already dead
            if (!CharacterHelpers.IsAlive(intent.Target))
                continue;

            // ---------- Phase 3: Reaction Phase ----------
            _reactionResolver.Resolve(_intents, resolvedHit);

            // ---------- Phase 4: Multi-hit continuation ----------
            if (!intent.IsReaction && resolvedHit.Result != AttackResultKind.Dodge && intent.RemainingHits > 1) // only continue multi-hit if it's not a reaction (to prevent infinite loops) and if the hit was not dodged (for more interesting combat) and if there are remaining hits
            {
                ref var nextMultiHitAttackIntent = ref _intents.Attack.Add();
                nextMultiHitAttackIntent.Attacker = intent.Attacker;
                nextMultiHitAttackIntent.Target = intent.Target;
                nextMultiHitAttackIntent.RemainingHits = intent.RemainingHits - 1;
                nextMultiHitAttackIntent.IsReaction = intent.IsReaction;
            }
        }
    }
}
