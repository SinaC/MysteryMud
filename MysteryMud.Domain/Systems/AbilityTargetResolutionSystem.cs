using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries;
using MysteryMud.GameData.Enums;
using System.Runtime.CompilerServices;

namespace MysteryMud.Domain.Systems;

public class AbilityTargetResolutionSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly AbilityRegistry _abilityRegistry;

    public AbilityTargetResolutionSystem(ILogger logger, IGameMessageService msg, IIntentContainer intents, AbilityRegistry abilityRegistry)
    {
        _logger = logger;
        _msg = msg;
        _intents = intents;
        _abilityRegistry = abilityRegistry;
    }

    // ROM Flag    Your System
    // TAR_IGNORE Scope = None OR Default = Self
    // TAR_CHAR_OFFENSIVE Allowed = Character + Default = CurrentOpponent
    // TAR_CHAR_DEFENSIVE Allowed = Character + Optional = false
    // TAR_CHAR_SELF Allowed = Self + Default = Self
    // TAR_OBJ_INV Allowed = Item
    // TAR_OBJ_CHAR_OFF Allowed = Item + Character + Default = CurrentOpponent
    // TAR_OBJ_CHAR_DEF Allowed = Item + Character + Default = Self

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.UseAbilitySpan)
        {
            var source = intent.Source;
            var abilityId = intent.AbilityId;
            var abilityKind = intent.AbilityKind;
            var targetKind = intent.TargetKind;
            var targetIndex = intent.TargetIndex;
            var targetName = intent.TargetName.AsSpan();

            if (!_abilityRegistry.TryGetValue(abilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                continue;
            }

            ref var targeting = ref abilityRuntime.Targeting;

            //
            List<Entity> targets;

            // TODO:
            // fix bash/kick
            //  use bash/kick without any target and not in combat -> it should fail
            // fix berserk <target>
            //  use berserk on a target should fail

            if (targeting.Scope == AbilityTargetScope.Room)
            {
                targets = FindAllInRoom(source, targeting.Allowed);
            }
            else
            {
                var primary = ResolvePrimary(source, targetKind, targetIndex, targetName, targeting);

                targets = targeting.Scope switch
                {
                    AbilityTargetScope.Single => primary,
                    AbilityTargetScope.Chain => primary.Count == 0
                        ? []
                        : ResolveChain(source, primary[0], targeting),
                    _ => primary
                };
            }

            // apply targets selector (none, random, lowest health, ...)
            targets = ApplySelection(targets, targeting);

            // keep targets satisfying each target validation rule
            targets = ApplyValidationRules(source, targets, abilityRuntime);

            // if no target or one target is not allowed, display message and stops
            if (!ValidateTargets(source, targets, targeting))
            {
                //if (targeting.Scope == AbilityTargetScope.Single) // display message only if single
                //{
                //    //EmitFailure(intent);
                //    _msg.To(source).Send("You don't see that here."); // TODO: custom message depending expected targets
                //}
                continue;
            }

            // add resolved ability intent
            ref var resolvedAbilityIntent = ref _intents.ResolvedAbility.Add();
            resolvedAbilityIntent.Source = source;
            resolvedAbilityIntent.Targets = targets;
            resolvedAbilityIntent.AbilityId = abilityId;
            resolvedAbilityIntent.AbilityKind = abilityKind;
            resolvedAbilityIntent.Cancelled = false;
        }
    }

    private List<Entity> FindAllInRoom(Entity source, AbilityTargetKindMask allowed) // TODO: use Allowed here instead of ValidateTargets step
    {
        // TODO: use allowed ?
        var room = source.Get<Location>().Room;
        var roomContents = room.Get<RoomContents>();

        return roomContents.Characters.Concat(roomContents.Items).Where(x => IsAllowed(source, x, allowed)).ToList();
    }

    private List<Entity> Find(Entity source, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName, AbilityTargetKindMask allowed) // TODO: use Allowed here instead of ValidateTargets step
    {
        // TODO: use allowed ?
        var room = source.Get<Location>().Room;
        var roomContents = room.Get<RoomContents>();

        var characters = EntityFinder.SelectTargets(source, targetKind, targetIndex, targetName, roomContents.Characters);
        var items = EntityFinder.SelectTargets(source, targetKind, targetIndex, targetName, roomContents.Items);

        return characters.Concat(items).Where(x => IsAllowed(source, x, allowed)).ToList();
    }

    private List<Entity> TryGetOpponent(Entity source)
    {
        ref var combatState = ref source.TryGetRef<CombatState>(out var isInCombat);
        if (!isInCombat)
            return [];
        return [combatState.Target];
    }

    private List<Entity> ResolvePrimary(Entity source, TargetKind targetKind, int targetIndex, ReadOnlySpan<char> targetName, AbilityTargeting targeting)
    {
        // 1. Explicit target
        if (!targetName.IsEmpty)
        {
            return Find(source, targetKind, targetIndex, targetName, targeting.Allowed);
        }

        // 2. Default resolution
        return targeting.Default switch
        {
            AbilityDefaultTargetRule.Self => [source],
            AbilityDefaultTargetRule.CurrentOpponent => TryGetOpponent(source),
            AbilityDefaultTargetRule.None => [],
            _ => []
        };
    }

    private bool ValidateTargets(Entity source, List<Entity> targets, AbilityTargeting targeting)
    {
        if (targets.Count == 0) // we need at least one target
            return false;

        foreach (var target in targets)
        {
            if (!IsAllowed(source, target, targeting.Allowed))
                return false;
        }

        return true;
    }

    private bool IsAllowed(Entity source, Entity target, AbilityTargetKindMask allowed)
    {
        if (allowed == AbilityTargetKindMask.None)
            return false;

        if (allowed == AbilityTargetKindMask.Any)
            return true;

        if (source == target && allowed.HasFlag(AbilityTargetKindMask.Self))
            return true;

        if (target.Has<CharacterTag>() && allowed.HasFlag(AbilityTargetKindMask.AnyCharacter))
            return true;

        // TODO: item
        if (target.Has<ItemTag>() && allowed.HasFlag(AbilityTargetKindMask.AnyItem))
            return true;

        return false;
    }

    private List<Entity> ApplySelection(List<Entity> targets, AbilityTargeting targeting)
    {
        if (targeting.MaxTargets <= 0 || targets.Count <= targeting.MaxTargets)
            return targets;

        // TODO
        //return targeting.Selection switch
        //{
        //    AbilityTargetSelection.None => targets,
        //    AbilityTargetSelection.Random => TakeRandom(targets, targeting.MaxTargets),
        //    AbilityTargetSelection.LowestHealth => TakeLowestHealth(targets, targeting.MaxTargets),
        //    _ => targets[..targeting.MaxTargets]
        //};
        return targets[..targeting.MaxTargets];
    }

    private List<Entity> ApplyValidationRules(Entity source, List<Entity> targets, AbilityRuntime abilityRuntime)
    {
        var validTargets = new List<Entity>();
        foreach (var target in targets)
        {
            var passed = true;
            foreach (var rule in abilityRuntime.TargetValidationRules)
            {
                var result = rule.Validate(target, abilityRuntime);

                if (!result.Success)
                {
                    if (result.FailActions.HasFlag(AbilityValidationRuleFailActions.DisplayMessage) && result.FailureMessageKey is not null)
                        SendAbilityMessage(source, target, abilityRuntime, result.FailureMessageKey);
                    passed = false;
                    break;
                }
            }
            // add target if passed each validation rules
            if (passed)
                validTargets.Add(target);
        }
        return validTargets;
    }

    private List<Entity> ResolveChain(Entity caster, Entity firstTarget, AbilityTargeting targeting)
    {
        // TODO
        return [firstTarget];
        //var results = new List<Entity>(targeting.MaxTargets);
        //var visited = new HashSet<Entity>();

        //results.Add(firstTarget);
        //visited.Add(firstTarget);

        //var current = firstTarget;

        //while (results.Count < targeting.MaxTargets)
        //{
        //    var candidates = EntityFinder.FindInRange(
        //        caster,
        //        current,
        //        range: 3, // configurable later
        //        targeting.Allowed
        //    );

        //    Entity next = Entity.Null;
        //    float bestScore = float.MaxValue;

        //    foreach (var c in candidates)
        //    {
        //        if (visited.Contains(c))
        //            continue;

        //        if (!ApplyFilters(caster, c, targeting.Filters))
        //            continue;

        //        var score = Distance(current, c);

        //        if (score < bestScore)
        //        {
        //            bestScore = score;
        //            next = c;
        //        }
        //    }

        //    if (next == Entity.Null)
        //        break;

        //    results.Add(next);
        //    visited.Add(next);
        //    current = next;
        //}

        //return results;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool ApplyFilters(Entity caster, Entity target, List<AbilityTargetFilterId> filters)
    {
        foreach (var f in filters)
        {
            if (!TargetFilterRegistry.Match(f, caster, target))
                return false;
        }
        return true;
    }

    private void SendAbilityMessage(Entity actor, Entity target, AbilityRuntime ability, string key)
    {
        if (key is null)
            return;
        if (ability.Messages.TryGetValue(key, out var msg))
            _msg.To(actor).Act(msg).With(target);
        else
            _logger.LogError("Ability {abilityName} validation rules refers to {key} but it's not found in messages", ability.Name, key);
    }
}


public interface ITargetFilter
{
    bool Match(Entity caster, Entity target);
}

public static class TargetFilterRegistry
{
    private static readonly Dictionary<AbilityTargetFilterId, ITargetFilter> _filters;

    public static bool Match(AbilityTargetFilterId id, Entity caster, Entity target)
        => _filters[id].Match(caster, target);
}

// example
//public struct EnemyOnlyFilter : ITargetFilter
//{
//    public bool Match(Entity caster, Entity target)
//        => FactionSystem.AreEnemies(caster, target);
//}

public struct NotSelfFilter : ITargetFilter
{
    public bool Match(Entity caster, Entity target)
        => caster != target;
}

public struct AliveFilter : ITargetFilter
{
    public bool Match(Entity caster, Entity target)
        => !target.Has<Dead>();
}