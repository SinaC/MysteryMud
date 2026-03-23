using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Domain.Components;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Domain.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems;

public static class RespawnSystem
{
    public static void Process(SystemContext systemContext, GameState state)
    {
        var query = new QueryDescription()
            .WithAll<PlayerTag, RespawnState, Location, Health>();
        state.World.Query(query, (Entity player, ref RespawnState respawnState, ref Location location, ref Health health) =>
        {
            // Optional: respawn timer check
            //if (Time.time - dead.TimeOfDeath >= RespawnDelay)
            {
                Logger.Logger.Respawn(player, respawnState.RespawnRoom);

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
                systemContext.MessageBus.Publish(player, "You have respawned!");
            }
        });
    }
}
