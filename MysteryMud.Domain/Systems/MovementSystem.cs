using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Ability;
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
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IIntentContainer _intentContainer;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IEventBuffer<RoomEnteredEvent> _roomEnteredEvent;

    public MovementSystem(ILogger logger, IGameMessageService msg, IDirtyTracker dirtyTracker, IIntentContainer intentContainer, IAbilityRegistry abilityRegistry, IEventBuffer<RoomEnteredEvent> roomEnteredEvent)
    {
        _logger = logger;
        _msg = msg;
        _dirtyTracker = dirtyTracker;
        _intentContainer = intentContainer;
        _abilityRegistry = abilityRegistry;
        _roomEnteredEvent = roomEnteredEvent;
    }

    public void Tick(GameState state)
    {
        foreach(ref var intent in _intentContainer.MoveSpan)
        {
            Move(state, intent);
        }
    }

    private void Move(GameState state, MoveIntent intent)
    {
        // TODO: we should probably validate the intent here, but for now we'll just assume it's valid and let it throw if it's not
        var movingEntity = intent.Actor;
        var fromRoom = intent.FromRoom;
        var toRoom = intent.ToRoom;
        var direction = intent.Direction;

        if (!MovementValidator.CanEnter(
                state.World, movingEntity,
                fromRoom, toRoom,
                direction, out var blockReason))
        {
            _msg.To(movingEntity).Act("You cannot go {0}: {1}").With(direction, blockReason);
            return;
        }

        ref var location = ref movingEntity.TryGetRef<Location>(out var hasLocation);
        if (hasLocation)
        {
            ref var oldRoomContents = ref fromRoom.Get<RoomContents>();
            ref var newRoomContents = ref toRoom.Get<RoomContents>();

            oldRoomContents.Characters.Remove(movingEntity);
            _msg.To(oldRoomContents.Characters).Act("{0} leaves {1}").With(movingEntity, direction); // entity will not receive the msg, but the other characters in the room will
            _msg.To(newRoomContents.Characters).Act("{0} has arrived").With(movingEntity); // entity will not receive the msg, but the other characters in the room will
            newRoomContents.Characters.Add(movingEntity);

            location.Room = toRoom;

            // remove casting and display phrase
            ref var casting = ref movingEntity.TryGetRef<Casting>(out var isCasting);
            if (isCasting)
            {
                movingEntity.Remove<Casting>();

                var abilityId = casting.AbilityId;
                if (!_abilityRegistry.TryGetRuntime(casting.AbilityId, out var abilityRuntime) || abilityRuntime == null)
                {
                    _logger.LogError("Ability {abilityId} not found", abilityId);
                    return;
                }

                _msg.To(movingEntity).Act(CastMessageHelpers.CasterInterruptMessage).With(abilityRuntime.Name);
                _msg.ToRoom(movingEntity).Act(CastMessageHelpers.RoomInterruptMessage).With(movingEntity);
            }

            if (movingEntity.Has<PlayerTag>())
                _dirtyTracker.MarkDirty(movingEntity, DirtyReason.CoreData);

            if (intent.AutoLook)
            {
                ref var lookIntent = ref _intentContainer.Look.Add();
                lookIntent.Viewer = movingEntity;
                lookIntent.TargetKind = LookTargetKind.Room;
                lookIntent.Target = toRoom;
                lookIntent.Mode = LookMode.PostUpdate;
            }

            // event
            ref var roomEnteredMovedEvt = ref _roomEnteredEvent.Add();
            roomEnteredMovedEvt.Entity = movingEntity;
            roomEnteredMovedEvt.FromRoom = fromRoom;
            roomEnteredMovedEvt.ToRoom = toRoom;
            roomEnteredMovedEvt.Direction = direction;
            roomEnteredMovedEvt.AutoLook = intent.AutoLook;
        }

        if (movingEntity.Has<CombatState>() && movingEntity.Has<PlayerTag>())
        {
            var target = movingEntity.Get<CombatState>().Target;
            CombatHelpers.ForfeitClaim(target, movingEntity);
        }
    }
}
