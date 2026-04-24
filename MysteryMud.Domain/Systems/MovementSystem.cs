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
using TinyECS;

namespace MysteryMud.Domain.Systems;

public class MovementSystem
{
    private World _world;
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly ICastMessageService _castMessageService;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IIntentContainer _intentContainer;
    private readonly IAbilityRegistry _abilityRegistry;
    private readonly IEventBuffer<RoomEnteredEvent> _roomEnteredEvent;

    public MovementSystem(World world, ILogger logger, IGameMessageService msg, ICastMessageService castMessageService, IDirtyTracker dirtyTracker, IIntentContainer intentContainer, IAbilityRegistry abilityRegistry, IEventBuffer<RoomEnteredEvent> roomEnteredEvent)
    {
        _world = world;
        _logger = logger;
        _msg = msg;
        _castMessageService = castMessageService;
        _dirtyTracker = dirtyTracker;
        _intentContainer = intentContainer;
        _abilityRegistry = abilityRegistry;
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
        var movingEntity = intent.Actor;
        var fromRoom = intent.FromRoom;
        var toRoom = intent.ToRoom;
        var direction = intent.Direction;

        ref var location = ref _world.TryGetRef<Location>(movingEntity, out var hasLocation);
        if (!hasLocation)
            return;

        // validate move
        if (!MovementValidator.CanEnter(
            _world,
            movingEntity,
            fromRoom, toRoom,
            direction, out var blockReason))
        {
            _msg.To(movingEntity).Act("You cannot go {0}: {1}").With(direction, blockReason);
            return;
        }

        // check move cost
        if (!MovementValidator.CanPayMoveCost(
            _world,
            movingEntity,
            fromRoom, toRoom,
            direction, out var moveCost))
        {
            _msg.To(movingEntity).Send("You are too exhausted.");
            return;
        }

        // pay move cost
        MovementValidator.PayMoveCost(_world, movingEntity, moveCost);

        // move
        ref var oldRoomContents = ref _world.Get<RoomContents>(fromRoom);
        ref var newRoomContents = ref _world.Get<RoomContents>(toRoom);

        oldRoomContents.Characters.Remove(movingEntity);
        _msg.To(oldRoomContents.Characters).Act("{0} leaves {1}").With(movingEntity, direction); // entity will not receive the msg, but the other characters in the room will
        _msg.To(newRoomContents.Characters).Act("{0} has arrived").With(movingEntity); // entity will not receive the msg, but the other characters in the room will
        newRoomContents.Characters.Add(movingEntity);

        // change location
        location.Room = toRoom;

        // 'consume' move
        ref var move = ref _world.Get<Move>(movingEntity);
        move.Current = move.Current - 10; // TODO: calculate move cost

        // remove casting and display phrase
        ref var casting = ref _world.TryGetRef<Casting>(movingEntity, out var isCasting);
        if (isCasting)
        {
            _world.Remove<Casting>(movingEntity);

            var abilityId = casting.AbilityId;
            if (!_abilityRegistry.TryGetRuntime(casting.AbilityId, out var abilityRuntime) || abilityRuntime == null)
            {
                _logger.LogError("Ability {abilityId} not found", abilityId);
                return;
            }

            _msg.To(movingEntity).Act(_castMessageService.CasterInterruptMessage).With(abilityRuntime.Name);
            _msg.ToRoom(movingEntity).Act(_castMessageService.RoomInterruptMessage).With(movingEntity);
        }

        if (_world.Has<PlayerTag>(movingEntity))
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

        // remove from combat and forfeit claim for player
        if (_world.Has<CombatState>(movingEntity))
        {
            var target = _world.Get<CombatState>(movingEntity).Target;
            if (_world.Has<PlayerTag>(movingEntity))
                CombatHelpers.ForfeitClaim(_world, target, movingEntity);
        }
    }
}
