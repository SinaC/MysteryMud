using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Systems;

public class MovementSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intentContainer;
    private readonly IEventBuffer<RoomEnteredEvent> _roomEnteredEvent;

    public MovementSystem(IGameMessageService msg, IIntentContainer intentContainer, IEventBuffer<RoomEnteredEvent> roomEnteredEvent)
    {
        _msg = msg;
        _intentContainer = intentContainer;
        _roomEnteredEvent = roomEnteredEvent;
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
        var movingEntity = intent.Actor;

        ref var location = ref movingEntity.TryGetRef<Location>(out var hasLocation);
        if (hasLocation)
        {
            ref var oldRoomContents = ref intent.FromRoom.Get<RoomContents>();
            ref var newRoomContents = ref intent.ToRoom.Get<RoomContents>();

            oldRoomContents.Characters.Remove(movingEntity);
            _msg.To(oldRoomContents.Characters).Act("{0} leaves {1}").With(movingEntity, intent.Direction); // entity will not receive the msg, but the other characters in the room will
            _msg.To(newRoomContents.Characters).Act("{0} has arrived").With(movingEntity); // entity will not receive the msg, but the other characters in the room will
            newRoomContents.Characters.Add(movingEntity);

            location.Room = intent.ToRoom;

            if (intent.AutoLook)
            {
                ref var lookIntent = ref _intentContainer.Look.Add();
                lookIntent.Viewer = movingEntity;
                lookIntent.TargetKind = LookTargetKind.Room;
                lookIntent.Target = intent.ToRoom;
                lookIntent.Mode = LookMode.PostUpdate;
            }

            // TODO: remove casting and display phrase

            // event
            ref var roomEnteredMovedEvt = ref _roomEnteredEvent.Add();
            roomEnteredMovedEvt.Entity = movingEntity;
            roomEnteredMovedEvt.FromRoom = intent.FromRoom;
            roomEnteredMovedEvt.ToRoom = intent.ToRoom;
            roomEnteredMovedEvt.Direction = intent.Direction;
            roomEnteredMovedEvt.AutoLook = intent.AutoLook;
        }

        if (movingEntity.Has<CombatState>() && movingEntity.Has<PlayerTag>())
        {
            var target = movingEntity.Get<CombatState>().Target;
            CharacterHelpers.ForfeitClaim(target, movingEntity);
        }
    }
}
