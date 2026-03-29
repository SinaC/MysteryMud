using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class FleeSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<FleeBlockedEvent> _fleeBlockedEvents;

    public FleeSystem(IGameMessageService msg, IIntentContainer intents, IEventBuffer<FleeBlockedEvent> fleeBlockedEvents)
    {
        _msg = msg;
        _intents = intents;
        _fleeBlockedEvents = fleeBlockedEvents;
    }

    public void Tick(GameState state)
    {
        // TODO: message $"You cannot flee: {e.Reason}."
        foreach (var flee in _intents.FleeSpan)
        {
            // TODO: check if still in the same room, if not, block flee

            // Check if entity is in combat
            var inCombat = flee.Entity.Has<CombatState>();
            if (!inCombat)
            {
                ref var evt = ref _fleeBlockedEvents.Add();
                evt.Entity = flee.Entity;
                evt.Reason = FleeBlockedReason.NotInCombat;
                return;
            }

            // Search for an exit from the current room
            ref var room = ref flee.Entity.Get<Location>().Room;
            ref var roomGraph = ref room.Get<RoomGraph>();

            if (roomGraph.Exits.Count == 0)
            {
                _msg.To(flee.Entity).Send("PANIC! You couldn't escape!");

                ref var evt = ref _fleeBlockedEvents.Add();
                evt.Entity = flee.Entity;
                evt.Reason = FleeBlockedReason.NoExit;
                return;
            }

            // TODO: flee could fail based on stats, random chance, etc.
            var toRoom = roomGraph.Exits[0].TargetRoom;

            // TODO: lose xp

            // remove from combat
            RemoveFromCombat(state.World, flee.Entity);

            _msg.To(flee.Entity).Send("You flee from combat!");
            _msg.ToRoom(flee.Entity).Act("{0} has fled!").With(flee.Entity);

            // Convert to MoveIntent
            ref var move = ref _intents.Move.Add();
            move.Actor = flee.Entity;
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
