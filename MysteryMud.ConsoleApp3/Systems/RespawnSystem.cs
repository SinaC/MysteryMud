using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

public static class RespawnSystem
{
    public static void RespawnPlayers(World world)
    {
        var query = new QueryDescription()
            .WithAll<PlayerTag, RespawnState, Position, Health>();
        world.Query(query, (Entity player, ref RespawnState respawnState, ref Position position, ref Health health) =>
        {
            // Optional: respawn timer check
            //if (Time.time - dead.TimeOfDeath >= RespawnDelay)
            {
                Logger.Logger.Respawn(player, respawnState.RespawnRoom);

                // Move player to respawn room
                position.Room = respawnState.RespawnRoom;

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

                MessageSystem.Broadcast(respawnState.RespawnRoom, $"{player.DisplayName} has respawned!"); // TODO: don't display for player
            }
        });
    }
}
