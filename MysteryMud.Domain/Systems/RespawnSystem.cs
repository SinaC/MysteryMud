using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Components.Characters.Players;

namespace MysteryMud.Domain.Systems;

public static class RespawnSystem
{
    public static void Process(SystemContext ctx, GameState state)
    {
        var query = new QueryDescription()
            .WithAll<PlayerTag, RespawnState, Location, Health>();
        state.World.Query(query, (Entity player, ref RespawnState respawnState, ref Location location, ref Health health) =>
        {
            // Optional: respawn timer check
            //if (Time.time - dead.TimeOfDeath >= RespawnDelay)
            {
                ctx.Log.LogInformation(LogEvents.Respawn, "Respawning character {playerName} to room {roomName}", player.DebugName, respawnState.RespawnRoom.DebugName);

                // Move player to respawn room
                location.Room = respawnState.RespawnRoom;

                // Add player back to RoomContents
                var roomContents = respawnState.RespawnRoom.Get<RoomContents>();
                roomContents.Characters.Add(player);

                // Reset health
                health.Current = health.Max;
                // TODO: other reset logic like status effects, inventory, etc.

                // Remove RespawnState and DeadTag so player can act again
                player.Remove<RespawnState, Dead>();

                // Add dirty stats to force stats update
                if (!player.Has<DirtyStats>())
                    player.Add<DirtyStats>();

                // TODO: send to room
                ctx.MessageBus.Publish(player, "You have respawned!");
            }
        });
    }
}
