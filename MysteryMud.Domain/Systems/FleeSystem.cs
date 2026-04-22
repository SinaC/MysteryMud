using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class FleeSystem
{
    private const int MaxFleeTries = 6;

    private readonly IRandom _random;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IExperienceService _experienceService;
    private readonly IEventBuffer<FleeBlockedEvent> _fleeBlockedEvents;

    public FleeSystem(IRandom random, IGameMessageService msg, IIntentContainer intents, IExperienceService experienceService, IEventBuffer<FleeBlockedEvent> fleeBlockedEvents)
    {
        _random = random;
        _msg = msg;
        _intents = intents;
        _experienceService = experienceService;
        _fleeBlockedEvents = fleeBlockedEvents;
    }

    public void Tick(GameState state)
    {
        foreach (ref var flee in _intents.FleeSpan)
        {
            // TODO: check if still in the same room, if not, block flee

            var entity = flee.Entity;

            // Must be in combat
            if (!entity.Has<CombatState>())
            {
                BlockFlee(entity, FleeBlockedReason.NotInCombat);
                return;
            }

            // Search for an exit from the current room
            if (!TryGetRandomRoom(entity, out var toRoom, out var blockedReason))
            {
                _msg.To(entity).Send("PANIC! You couldn't escape!");
                BlockFlee(entity, blockedReason!.Value);
                return;
            }

            // Lose xp
            _experienceService.GrantExperience(entity, -10);

            // Remove from combat
            RemoveFromCombat(state, entity);

            _msg.To(entity).Send("You flee from combat!");
            _msg.ToRoom(entity).Act("{0} has fled!").With(entity);

            // Convert to MoveIntent
            ref var move = ref _intents.Move.Add();
            move.Actor = entity;
            move.FromRoom = flee.FromRoom;
            move.ToRoom = toRoom!.Value;
            move.AutoLook = true;
        }
    }

    private bool TryGetRandomRoom(Entity entity, out Entity? toRoom, out FleeBlockedReason? reason)
    {
        ref var room = ref entity.Get<Location>().Room;
        ref var roomGraph = ref room.Get<RoomGraph>();

        // Check if any exit exists at all
        var hasAnyExit = roomGraph.Exits.HasAnyExit();
        if (!hasAnyExit)
        {
            toRoom = null;
            reason = FleeBlockedReason.NoExit;
            return false;
        }

        for (var attempt = 0; attempt < MaxFleeTries; attempt++)
        {
            var randomDirection = _random.Pick<DirectionKind>();
            var exit = roomGraph.Exits[randomDirection];
            if (exit == null)
                continue;

            if (MovementValidator.CanFlee(entity, room, exit.Value.TargetRoom, randomDirection))
            {
                toRoom = exit.Value.TargetRoom;
                reason = null;
                return true;
            }
        }

        toRoom = null;
        reason = FleeBlockedReason.FailedToFlee;
        return false;
    }

    private void BlockFlee(Entity entity, FleeBlockedReason reason)
    {
        ref var evt = ref _fleeBlockedEvents.Add();
        evt.Entity = entity;
        evt.Reason = reason;
    }

    // remove from combat and forfeit claim for player
    private static void RemoveFromCombat(GameState state, Entity fleeingEntity)
    {
        var previousTarget = fleeingEntity.Get<CombatState>().Target;

        CombatHelpers.RemoveFromAllCombat(state, fleeingEntity);

        if (fleeingEntity.Has<PlayerTag>())
            CombatHelpers.ForfeitClaim(previousTarget, fleeingEntity);
    }
}
