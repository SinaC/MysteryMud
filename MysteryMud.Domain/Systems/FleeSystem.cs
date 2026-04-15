using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class FleeSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IExperienceService _experienceService;
    private readonly IEventBuffer<FleeBlockedEvent> _fleeBlockedEvents;

    public FleeSystem(IGameMessageService msg, IIntentContainer intents, IExperienceService experienceService, IEventBuffer<FleeBlockedEvent> fleeBlockedEvents)
    {
        _msg = msg;
        _intents = intents;
        _experienceService = experienceService;
        _fleeBlockedEvents = fleeBlockedEvents;
    }

    public void Tick(GameState state)
    {
        // TODO: message $"You cannot flee: {e.Reason}."
        foreach (ref var flee in _intents.FleeSpan)
        {
            // TODO: check if still in the same room, if not, block flee

            var entity = flee.Entity;

            // Check if entity is in combat
            var inCombat = entity.Has<CombatState>();
            if (!inCombat)
            {
                ref var evt = ref _fleeBlockedEvents.Add();
                evt.Entity = entity;
                evt.Reason = FleeBlockedReason.NotInCombat;
                return;
            }

            // Search for an exit from the current room
            ref var room = ref entity.Get<Location>().Room;
            ref var roomGraph = ref room.Get<RoomGraph>();

            if (roomGraph.Exits.Count == 0)
            {
                _msg.To(entity).Send("PANIC! You couldn't escape!");

                ref var evt = ref _fleeBlockedEvents.Add();
                evt.Entity = entity;
                evt.Reason = FleeBlockedReason.NoExit;
                return;
            }

            // TODO: flee could fail based on stats, random chance, etc.
            var toRoom = roomGraph.Exits[0].TargetRoom;

            // lose xp
            _experienceService.GrantExperience(entity, -50);

            // remove from combat
            RemoveFromCombat(state.World, entity);

            _msg.To(entity).Send("You flee from combat!");
            _msg.ToRoom(entity).Act("{0} has fled!").With(entity);

            // Convert to MoveIntent
            ref var move = ref _intents.Move.Add();
            move.Actor = entity;
            move.FromRoom = flee.FromRoom;
            move.ToRoom = toRoom;
            move.AutoLook = true;
        }
    }

    private static void RemoveFromCombat(World world, Entity victim)
    {
        // remove from combat
        victim.Remove<CombatState>();
        // remove combat state for anyone targeting this entity
        var query = new QueryDescription()
          .WithAll<CombatState>();
        world.Query(query, (Entity actor, ref CombatState combat) =>
        {
            if (combat.Target == victim)
                actor.Remove<CombatState>();
        });
    }
}
