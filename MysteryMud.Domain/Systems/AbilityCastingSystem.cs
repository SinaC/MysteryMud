using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Helpers;

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
        // TODO: interrupt: Your concentration is broken! You fail to cast '{spellName}'.
        // TODO: use scheduler instead of looping each tick
        var query = new QueryDescription()
            .WithAll<Casting>()
            .WithNone<Dead>();
        state.World.Query(query, (Entity entity, ref Casting casting) =>
        {
            var abilityId = casting.AbilityId;
            var source = casting.Source;

            // casting time elapsed ?
            if (state.CurrentTick < casting.ExecuteAt)
            {
                if (state.CurrentTick - casting.LastUpdate >= 5 )
                {
                    _msg.To(source).Send(CastMessageHelpers.CasterTickMessage);
                    _msg.ToRoom(source).Act(CastMessageHelpers.RoomTickMessage).With(source);
                    casting.LastUpdate = state.CurrentTick;
                }
                return;
            }

            if (!_abilityRegistry.TryGetValue(casting.AbilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                return;
            }

            _msg.To(source).Act(CastMessageHelpers.CasterFinishMessage).With(abilityRuntime.Name);
            _msg.ToRoom(source).Act(CastMessageHelpers.RoomFinishMessage).With(source);

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