using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Combat.Factories;
using MysteryMud.Domain.Combat.Resolvers;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Combat;

public sealed class CombatOrchestrator
{
    private readonly IIntentContainer _intents;
    private readonly DamageResolver _damageResolver;
    private readonly HitResolver _hitResolver;
    private readonly DamageFactory _damageProducer;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;

    public CombatOrchestrator(IGameMessageService msg, IIntentContainer intents, IEventBuffer<DamageEvent> damages, DamageResolver damageResolver)
    {
        _intents = intents;
        _damageResolver = damageResolver;
        _hitResolver = new HitResolver(msg); // TODO: don't create instance, inject it instead
        _damageProducer = new DamageFactory(damages); // TODO: don't create instance, inject it instead
        _weaponProcResolver = new WeaponProcResolver(); // TODO: don't create instance, inject it instead
        _reactionResolver = new ReactionResolver(msg); // TODO: don't create instance, inject it instead
    }

    public void Tick(GameState gameState)
    {
        // TODO: it would nicer if we could use intentBusContainer instead of this hitQueue
        // we need to resolve intents in a loop until there are no more intents to properly handle reactions between hits, for example if we have 2 entities A and B, A attacks B with 3 hits and B has a chance to counterattack on hit, we want the flow to be like this:
        var hitQueue = new Queue<AttackIntent>();
        foreach (var attackIntent in _intents.AttackSpan)
        {
            hitQueue.Enqueue(attackIntent);
        }

        // resolve one combat intent at a time to properly handle reactions between hits
        while (hitQueue.Count > 0) // TODO: limit max iterations to prevent infinite loops in case of bugs
        {
            var intent = hitQueue.Dequeue();
            if (!CharacterHelpers.IsAlive(intent.Attacker, intent.Target))
                continue;

            // ---------- Phase 1: Resolve Hit ----------
            var resolvedHit = _hitResolver.Resolve(intent);

            // ---------- Phase 2: Produce Damage ----------
            // if hit is successful, produce damage and handle reactions before resolving next hit, this way we can properly handle counterattacks between hits instead of waiting for all hits to be resolved and then handling reactions which can lead to unrealistic scenarios like player attacking 3 times and then monster counterattacking 3 times even if the player is already dead after the first hit
            if (resolvedHit.Result == AttackResults.Hit)
            {
                ref var damageEvent = ref _damageProducer.CreateHit(resolvedHit);

                _damageResolver.Resolve(damageEvent);

                // ---------- Weapon procs (immediate on hit) ----------
                // TODO: handle weapon proc
            }

            // stop iteration if target is dead, no need to resolve reactions or continue multi-hit if target is already dead
            if (!CharacterHelpers.IsAlive(intent.Target))
                continue;

            // ---------- Phase 3: Reaction Phase ----------
            _reactionResolver.Handle(hitQueue, resolvedHit);

            // ---------- Phase 4: Multi-hit continuation ----------
            if (!intent.IsReaction && resolvedHit.Result != AttackResults.Dodge && intent.RemainingHits > 1) // only continue multi-hit if it's not a reaction (to prevent infinite loops) and if the hit was not dodged (for more interesting combat) and if there are remaining hits
            {
                hitQueue.Enqueue(new AttackIntent
                {
                    Attacker = intent.Attacker,
                    Target = intent.Target,
                    RemainingHits = intent.RemainingHits - 1,
                    IsReaction = intent.IsReaction
                });
            }
        }
    }
}
