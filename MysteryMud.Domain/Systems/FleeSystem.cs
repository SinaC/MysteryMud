using Arch.Core.Extensions;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class FleeSystem
{
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<FleeBlockedEvent> _fleeBlockedEvents;

    public FleeSystem(IIntentContainer intents, IEventBuffer<FleeBlockedEvent> fleeBlockedEvents)
    {
        _intents = intents;
        _fleeBlockedEvents = fleeBlockedEvents;
    }

    public void Tick()
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
                ref var evt = ref _fleeBlockedEvents.Add();
                evt.Entity = flee.Entity;
                evt.Reason = FleeBlockedReason.NoExit;
                return;
            }

            // TODO: flee could fail based on stats, random chance, etc.
            var toRoom = roomGraph.Exits[0].TargetRoom;

            // Convert to MoveIntent
            ref var move = ref _intents.Move.Add();
            move.Actor = flee.Entity;
            move.FromRoom = flee.FromRoom;
            move.ToRoom = toRoom;
        }
    }
}
