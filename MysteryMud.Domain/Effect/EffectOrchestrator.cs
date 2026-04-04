using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Domain.Damage;
using MysteryMud.Domain.Effect.Factories;
using MysteryMud.Domain.Heal;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;
using System.Runtime.CompilerServices;

namespace MysteryMud.Domain.Effect;

// will be called from GameLoop and also from CombatOrchestrator to handle weapon proc
public class EffectOrchestrator
{
    private readonly ILogger _logger;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<EffectResolvedEvent> _effectResolved;
    private readonly SpellDatabase _spellDatabase;
    private readonly EffectFactory _effectFactory;
    private readonly DamageResolver _damageResolver;
    private readonly HealResolver _healResolver;

    public EffectOrchestrator(ILogger logger, IIntentContainer intents, IEventBuffer<EffectResolvedEvent> effectResolved, SpellDatabase spellDatabase, EffectFactory effectFactory, DamageResolver damageResolver, HealResolver healResolver)
    {
        _logger = logger;
        _intents = intents;
        _effectResolved = effectResolved;
        _spellDatabase = spellDatabase;
        _effectFactory = effectFactory;
        _damageResolver = damageResolver;
        _healResolver = healResolver;
    }

    public void Tick(GameState state)
    {
        for (int i = 0; i < _intents.EffectCount; i++) // we iterate using index to be able to add intents while iterating
        {
            var intent = _intents.EffectByIndex(i);
            if (intent.Cancelled)
                continue;

            ResolveImmediate(state, ref intent);
        }
    }

    public void ResolveImmediate(GameState state, ref EffectIntent intent)
    {
        if (!_spellDatabase.EffectsById.TryGetValue(intent.EffectId, out var effectDefinition))
        {
            _logger.LogError("Effect id {effectId} not found in the effect database", intent.EffectId);
            return;
        }

        // TODO: other effects (direct damage, direct heal, ...)
        _effectFactory.ApplyEffect(state, effectDefinition, intent.Source, intent.Target);
    }
}
