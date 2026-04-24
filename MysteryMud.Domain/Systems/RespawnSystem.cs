using MysteryMud.Core;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class RespawnSystem
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;

    public RespawnSystem(World world, IGameMessageService msg, IDirtyTracker dirtyTracker)
    {
        _world = world;
        _msg = msg;
        _dirtyTracker = dirtyTracker;
    }

    private static readonly QueryDescription _respawnRequiredQueryDesc = new QueryDescription()
        .WithAll<PlayerTag, RespawnState, Location>(); // TODO: add health, move when query on 5 is implemented

    public void Tick(GameState state)
    {
        _world.Query(_respawnRequiredQueryDesc, (EntityId player,
            ref PlayerTag playerTag,
            ref RespawnState respawnState,
            ref Location location) =>
        /*, TODO when Query on 5 is implemented
            ref Health health,
            ref Move move*/
        {
            // Optional: respawn timer check
            //if (Time.time - dead.TimeOfDeath >= RespawnDelay)
            {
                // remove player from location
                ref var deathRoomContents = ref _world.Get<RoomContents>(location.Room);
                deathRoomContents.Characters.Remove(player);

                // Move player to respawn room
                location.Room = respawnState.RespawnRoom;

                // Add player back to RoomContents
                ref var respawnRoomContents = ref _world.Get<RoomContents>(respawnState.RespawnRoom);
                respawnRoomContents.Characters.Add(player);

                ref var health = ref _world.Get<Health>(player);
                ref var move = ref _world.Get<Move>(player);

                // Reset health and move to 1
                health.Current = 1;
                move.Current = 1;

                // TODO: other reset logic like status effects, inventory, etc.

                // Remove RespawnState and DeadTag so player can act again
                _world.Remove<RespawnState>(player);
                if (_world.Has<Dead>(player))
                    _world.Remove<Dead>(player);

                // Add dirty stats to force stats update
                if (!_world.Has<DirtyStats>(player))
                    _world.Add<DirtyStats>(player);

                // TODO: send to room
                _msg.To(player).Act("You have respawned at {0}!").With(location.Room);

                _dirtyTracker.MarkDirty(player, DirtyReason.Respawn);
            }
        });
    }
}
