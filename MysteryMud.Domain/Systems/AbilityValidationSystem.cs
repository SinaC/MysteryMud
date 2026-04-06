using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Resources;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class AbilityValidationSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly AbilityRegistry _abilityRegistry;

    public AbilityValidationSystem(ILogger logger, IGameMessageService msg, IIntentContainer intents, AbilityRegistry abilityRegistry)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _abilityRegistry = abilityRegistry;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.UseAbilitySpan)
        {
            var source = intent.Source;
            var abilityId = intent.AbilityId;
            var targets = intent.Targets;

            if (!_abilityRegistry.TryGetValue(abilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                continue;
            }

            // already casting a spell
            if (source.Has<Casting>())
                continue;

            // TODO
            //if (IsOnCooldown(intent.User, def))
            //    continue;

            // check if has enough resources
            if (!ResourceHelpers.CanPayCosts(source, abilityRuntime, out var cannotPayResult))
            {
                var msg = GenerateCannotPayCostsMessage(cannotPayResult);
                _msg.To(source).Send(msg);
                continue;
            }

            // pay resource costs
            ResourceHelpers.PayCosts(source, abilityRuntime);

            // casting time ?
            if (abilityRuntime.CastTime > 0)
            {
                // set Casting component
                var executeAt = state.CurrentTick + abilityRuntime.CastTime;
                // TODO: use scheduler

                _msg.To(source).Act(CastMessageHelpers.CasterStartMessage).With(abilityRuntime.Name);
                _msg.ToRoom(source).Act(CastMessageHelpers.RoomStartMessage).With(source);

                source.Add(new Casting
                {
                    Source = source,
                    Targets = targets,
                    AbilityId = abilityId,
                    ExecuteAt = executeAt,
                    LastUpdate = state.CurrentTick,
                });

                continue;
            }

            // instant execution
            _msg.To(source).Act(CastMessageHelpers.CasterInstantMessage).With(abilityRuntime.Name);
            _msg.ToRoom(source).Act(CastMessageHelpers.RoomInstantMessage).With(source);
            // add execute ability intent
            ref var executeAbilityIntent = ref _intents.ExecuteAbility.Add();
            executeAbilityIntent.AbilityId = abilityRuntime.Id;
            executeAbilityIntent.Source = source;
            executeAbilityIntent.Targets = targets;
        }
    }

    private static string GenerateCannotPayCostsMessage(CannotPayCostsResult result)
        => result.Reason switch
        {
            CannotPayCostsReason.ResourceNotAvailable => result.Kind switch
            {
                ResourceKind.Mana => "You cannot use mana in your current form.",
                ResourceKind.Rage => "You cannot build rage right now.",
                ResourceKind.Energy => "You cannot use energy in your current form.",
                _ => "That resource is unavailable."
            },
            CannotPayCostsReason.NotEnoughResource => result.Kind switch
            {
                ResourceKind.Mana => "You don't have enough mana.",
                ResourceKind.Rage => "You don't have enough rage.",
                ResourceKind.Energy => "You don't have enough energy.",
                _ => "You don't have enough resources."
            },
            _ => "You don't have enough resources."
        };
}
