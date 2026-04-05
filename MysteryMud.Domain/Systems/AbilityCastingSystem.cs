using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Systems;

public class AbilityCastingSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly AbilityRegistry _abilityRegistry;

    public AbilityCastingSystem(ILogger logger, IGameMessageService msg, IIntentContainer intents, AbilityRegistry abilityRegistry)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _abilityRegistry = abilityRegistry;
    }

    public void Tick(GameState state)
    {
        // TODO: use scheduler instead of looping each tick
        var query = new QueryDescription()
            .WithAll<Casting>()
            .WithNone<Dead>();
        state.World.Query(query, (Entity entity, ref Casting casting) =>
        {
            // casting time elapsed ?
            if (state.CurrentTick < casting.ExecuteAt)
                return;

            var abilityId = casting.AbilityId;
            if (!_abilityRegistry.TryGetValue(casting.AbilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                return;
            }

            _msg.ToAll(casting.Source).Act("{0} finish{0:v} casting '{1}'").With(casting.Source, abilityRuntime.Name); // TODO: target
            // add execute ability intent
            ref var executeAbilityIntent = ref _intents.ExecuteAbility.Add();
            executeAbilityIntent.AbilityId = casting.AbilityId;
            executeAbilityIntent.Source = casting.Source;
            executeAbilityIntent.Targets = casting.Targets;

            // not casting anymore
            entity.Remove<Casting>();
        });
   }
}