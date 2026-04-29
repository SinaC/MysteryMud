using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;

namespace MysteryMud.Domain.Systems;

public class RespawnSystem
{
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly EntitySet _waitingRespawnPlayersEntitySet;

    public RespawnSystem(World world, IGameMessageService msg, IDirtyTracker dirtyTracker)
    {
        _msg = msg;
        _dirtyTracker = dirtyTracker;
        _waitingRespawnPlayersEntitySet = world
            .GetEntities()
            .With<PlayerTag>()
            .With<RespawnState>()
            .AsSet();
    }

    public void Tick(GameState state)
    {
        foreach (var player in _waitingRespawnPlayersEntitySet.GetEntities())
        {
            RespawnPlayer(state, player);
        }
    }

    private void RespawnPlayer(GameState state, Entity player)
    {
        ref var location = ref player.Get<Location>();
        ref var respawnState = ref player.Get<RespawnState>();
        ref var health = ref player.Get<Health>();
        ref var move = ref player.Get<Move>();

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

            // Reset health and move to 1
            health.Current = 1;
            move.Current = 1;

            // TODO: other reset logic like status effects, inventory, etc.

            // Remove RespawnState and DeadTag so player can act again
            player.Remove<RespawnState>();
            if (player.Has<DeadTag>())
                player.Remove<DeadTag>();

            // Add dirty stats to force stats update
            if (!player.Has<DirtyStats>())
                player.Set<DirtyStats>();

            // TODO: send to room
            _msg.To(player).Act("You have respawned at {0}!").With(location.Room);

            _dirtyTracker.MarkDirty(player, DirtyReason.Respawn);
        }
    }
}
