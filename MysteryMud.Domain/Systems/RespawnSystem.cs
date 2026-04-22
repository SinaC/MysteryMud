using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;

namespace MysteryMud.Domain.Systems;

public class RespawnSystem
{
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;

    public RespawnSystem(IGameMessageService msg, IDirtyTracker dirtyTracker)
    {
        _msg = msg;
        _dirtyTracker = dirtyTracker;
    }

    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<PlayerTag, RespawnState, Location, Health>();
        state.World.Query(query, (Entity player, ref RespawnState respawnState, ref Location location, ref Health health) =>
        {
            // Optional: respawn timer check
            //if (Time.time - dead.TimeOfDeath >= RespawnDelay)
            {
                // remove player from location
                ref var deathRoomContents = ref location.Room.Get<RoomContents>();
                deathRoomContents.Characters.Remove(player);

                // Move player to respawn room
                location.Room = respawnState.RespawnRoom;

                // Add player back to RoomContents
                ref var respawnRoomContents = ref respawnState.RespawnRoom.Get<RoomContents>();
                respawnRoomContents.Characters.Add(player);

                // Reset health
                health.Current = health.Max;
                // TODO: other reset logic like status effects, inventory, etc.

                // Remove RespawnState and DeadTag so player can act again
                player.Remove<RespawnState, Dead>();

                // Add dirty stats to force stats update
                if (!player.Has<DirtyStats>())
                    player.Add<DirtyStats>();

                // TODO: send to room
                _msg.To(player).Send($"You have respawned at {location.Room.DisplayName}!");

                _dirtyTracker.MarkDirty(player, DirtyReason.Respawn);
            }
        });
    }
}
