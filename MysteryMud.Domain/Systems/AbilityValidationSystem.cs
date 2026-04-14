using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Ability.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Resources;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AbilityValidationSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<AbilityUsedEvent> _abilityUsed;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IAbilityOutcomeResolverRegistry _abilityOutcomeResolverRegistry;
    private readonly IAbilityTargetResolver _abilityTargetResolver;

    public AbilityValidationSystem(ILogger logger, IGameMessageService msg, IIntentContainer intents, IEventBuffer<AbilityUsedEvent> abilityUsed, IAbilityRegistry abilityRegistry, IAbilityOutcomeResolverRegistry abilityOutcomeResolverRegistry, IAbilityTargetResolver abilityTargetResolver)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _abilityUsed = abilityUsed;
        _abilityRegistry = abilityRegistry;
        _abilityOutcomeResolverRegistry = abilityOutcomeResolverRegistry;
        _abilityTargetResolver = abilityTargetResolver;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.UseAbilitySpan)
        {
            var abilityId = intent.AbilityId;
            var source = intent.Source;
            var targetKind = intent.TargetKind;
            var targetIndex = intent.TargetIndex;
            var targetName = intent.TargetName;

            if (!_abilityRegistry.TryGetRuntime(abilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                continue;
            }

            // check source validation rules
            if (!ValidateSource(abilityRuntime, source))
            {
                intent.Cancelled = true;
                continue;
            }

            // cancelled ?
            if (intent.Cancelled)
                continue;

            // already casting a spell
            ref var casting = ref source.TryGetRef<Casting>(out var isCasting);
            if (isCasting)
            {
                _msg.To(source).Send($"You are already focused on {abilityRuntime.Name}");
                continue;
            }

            // Resolve targets(Single always at CastStart)
            List<Entity>? resolvedTargets = null;

            if (abilityRuntime.Targeting.Selection == AbilityTargetSelection.Single ||
                abilityRuntime.Targeting.ResolveAt == AbilityTargetResolveAt.CastStart)
            {
                var result = _abilityTargetResolver.Resolve(source, targetKind, targetIndex, targetName, abilityRuntime.Targeting, state);

                if (result.Status != TargetResolutionStatus.Ok)
                {
                    _msg.To(source).Send(result.FailureMessage ?? "Invalid target.");
                    continue;
                }

                // Apply target validation rules; for single-target any failure aborts
                var filtered = FilterTargets(abilityRuntime, result.Targets, source, abortOnFirst: abilityRuntime.Targeting.Selection == AbilityTargetSelection.Single);
                if (filtered is null)
                    continue;   // abort signalled
                resolvedTargets = filtered;
            }

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

            // check if ability directly fails (skill learned % for example)
            if (abilityRuntime.OutcomeResolver is { Hook: AbilityOutcomeHook.Validation })
            {
                if (_abilityOutcomeResolverRegistry.TryGetResolver(abilityRuntime.OutcomeResolver.ResolverId, out var registedResolver) && registedResolver is not null)
                {
                    var result = registedResolver.Resolver.Resolve(source, abilityRuntime);
                    SendAbilityMessage(source, abilityRuntime, result.Outcome);
                    if (!result.Success)
                        continue;
                }
            }

            // add ability used event
            ref var abilityUsedEvt = ref _abilityUsed.Add();
            abilityUsedEvt.AbilityId = abilityId;
            abilityUsedEvt.Source = source;

            // instant vs delayed cast
            var isDelayed = abilityRuntime.CastTime > 0;

            // delayed cast ?
            if (isDelayed)
            {
                // set Casting component
                var executeAt = state.CurrentTick + abilityRuntime.CastTime;
                // TODO: use scheduler

                _msg.To(source).Act(CastMessageHelpers.CasterStartMessage).With(abilityRuntime.Name);
                _msg.ToRoom(source).Act(CastMessageHelpers.RoomStartMessage).With(source);

                source.Add(new Casting
                {
                    AbilityId = abilityId,
                    Source = source,
                    ResolvedTargets = resolvedTargets, // null for AoE CastCompletion
                    ExecuteAt = executeAt,
                    LastUpdate = state.CurrentTick,
                });

                continue;
            }

            // instant cast

            // For instant AoE that deferred resolution, resolve now
            if (resolvedTargets is null)
            {
                var result = _abilityTargetResolver.Resolve(source, targetKind, targetIndex, targetName, abilityRuntime.Targeting, state);
                resolvedTargets = result.Status == TargetResolutionStatus.Ok
                    ? FilterTargets(abilityRuntime, result.Targets, source, abortOnFirst: false) ?? []
                    : [];
            }

            // message for spells
            if (abilityRuntime.Kind == AbilityKind.Spell) // send generic randomized message for spell
            {
                _msg.To(source).Act(CastMessageHelpers.CasterInstantMessage).With(abilityRuntime.Name);
                _msg.ToRoom(source).Act(CastMessageHelpers.RoomInstantMessage).With(source);
            }

            // add execute ability intent
            ref var executeAbilityIntent = ref _intents.ExecuteAbility.Add();
            executeAbilityIntent.AbilityId = abilityRuntime.Id;
            executeAbilityIntent.Source = source;
            executeAbilityIntent.Targets = resolvedTargets;
        }
    }

    // Returns filtered list, or null if an Abort-rule fired
    private List<Entity>? FilterTargets(
        AbilityRuntime ability,
        List<Entity> candidates,
        Entity source,
        bool abortOnFirst)
    {
        var passed = new List<Entity>(candidates.Count);

        foreach (var target in candidates)
        {
            bool skip = false;

            foreach (var rule in ability.TargetValidationRules)
            {
                var result = rule.Validate(target);
                if (result.Success) continue;

                switch (result.FailBehaviour)
                {
                    case AbilityValidationFailBehaviour.Abort:
                        if (result.FailMessageKey is not null)
                            SendAbilityMessage(source, ability, result.FailMessageKey);
                        if (abortOnFirst) return null;
                        skip = true;
                        break;

                    case AbilityValidationFailBehaviour.SkipWithMessage:
                        if (result.FailMessageKey is not null)
                            SendAbilityMessage(source, ability, result.FailMessageKey);
                        skip = true;
                        break;

                    case AbilityValidationFailBehaviour.Skip:
                        skip = true;
                        break;
                }

                if (skip) break;
            }

            if (!skip) passed.Add(target);
        }

        return passed;
    }

    private bool ValidateSource(AbilityRuntime ability, Entity source)
    {
        foreach (var rule in ability.SourceValidationRules)
        {
            var result = rule.Validate(source);
            if (result.Success) continue;

            if (result.FailMessageKey is not null)
                SendAbilityMessage(source, ability, result.FailMessageKey);

            return false;
        }
        return true;
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

    private void SendAbilityMessage(Entity actor, AbilityRuntime ability, string key) // TODO: same code found in AbilityExecutionSystem/AbilityCastingSystem
    {
        if (key is null)
            return;
        if (ability.Messages.TryGetValue(key, out var msg))
            _msg.To(actor).Send(msg);
        else
            _logger.LogError("Ability {abilityName} validation rules refers to {key} but it's not found in messages", ability.Name, key);
    }
}
