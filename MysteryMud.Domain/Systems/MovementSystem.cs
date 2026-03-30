using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Systems;

public class MovementSystem
{
    private IGameMessageService _msg;
    private IIntentContainer _intentContainer;
    private IEventBuffer<MovedEvent> _movedEvents;

    public MovementSystem(IGameMessageService msg, IIntentContainer intentContainer, IEventBuffer<MovedEvent> movedEvents)
    {
        _msg = msg;
        _intentContainer = intentContainer;
        _movedEvents = movedEvents;
    }

    public void Tick(GameState state)
    {
        foreach(ref var intent in _intentContainer.MoveSpan)
        {
            Move(intent);
        }
    }

    private void Move(MoveIntent intent)
    {
        // TODO: we should probably validate the intent here, but for now we'll just assume it's valid and let it throw if it's not

        ref var location = ref intent.Actor.TryGetRef<Location>(out var hasLocation);
        if (hasLocation)
        {
            ref var oldRoomContents = ref intent.FromRoom.Get<RoomContents>();
            ref var newRoomContents = ref intent.ToRoom.Get<RoomContents>();

            oldRoomContents.Characters.Remove(intent.Actor);
            _msg.To(oldRoomContents.Characters).Act("{0} leaves {1}").With(intent.Actor, intent.Direction); // entity will not receive the msg, but the other characters in the room will
            _msg.To(newRoomContents.Characters).Act("{0} has arrived").With(intent.Actor); // entity will not receive the msg, but the other characters in the room will
            newRoomContents.Characters.Add(intent.Actor);

            location.Room = intent.ToRoom;

            if (intent.AutoLook)
            {
                ref var lookIntent = ref _intentContainer.Look.Add();
                lookIntent.Viewer = intent.Actor;
                lookIntent.TargetKind = LookTargetKind.Room;
                lookIntent.Target = intent.ToRoom;
                lookIntent.Mode = LookMode.PostUpdate;
            }

            // event
            ref var movedEvt = ref _movedEvents.Add();
            movedEvt.Actor = intent.Actor;
            movedEvt.FromRoom = intent.FromRoom;
            movedEvt.ToRoom = intent.ToRoom;
            movedEvt.Direction = intent.Direction;
            movedEvt.AutoLook = intent.AutoLook;
        }
    }
}
