using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Factories;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DeathSystem
{
    public static void Process(World world)
    {
        var query = new QueryDescription()
          .WithAll<Dead>();
        world.Query(query, (Entity entity, ref Dead dead) =>
        {
            HandleDeath(world, entity, dead.Killer); // TODO: pass killer
        });
    }

    public static void HandleDeath(World world, Entity victim, Entity killer)
    {
        //TODO: log
        CreateCorpse(world, victim, killer);

        AddTags(world, victim);
        RemoveFromRoomContents(world, victim);
        RemoveFromCombat(world, victim);
        RemoveEffects(world, victim);
    }

    private static void AddTags(World world, Entity victim)
    {
        victim.Add<Dead>(); // mark as dead
        // player will respawn, NPCs will be cleaned up by CleanupSystem
        if (victim.Has<PlayerTag>())
            victim.Add(new RespawnState { RespawnRoom = WorldFactory.RespawnRoomEntity });
    }

    private static void RemoveFromRoomContents(World world, Entity victim)
    {
        if (!victim.Has<Position>())
            return; // can't remove from room contents if we don't know where the victim is
        ref var position = ref victim.Get<Position>();
        ref var roomContents = ref position.Room.Get<RoomContents>();
        roomContents.Characters.Remove(victim);
    }

    private static void RemoveFromCombat(World world, Entity victim)
    {
        // remove from combat
        victim.Remove<CombatState>();
        // remove combat state for anyone targeting this entity
        var query = new QueryDescription()
          .WithAll<CombatState>();
        world.Query(query, (Entity actor, ref CombatState combat) =>
        {
            if (combat.Target == victim)
                actor.Remove<CombatState>();
        });
    }

    private static void RemoveEffects(World world, Entity victim)
    {
        // remove all effects on victim
        ref var characterEffects = ref victim.Get<CharacterEffects>();
        foreach(var effect in characterEffects.Effects)
            world.Destroy(effect);
    }

    // TODO: create a corpse entity that can hold the items instead of dropping items on the floor
    private static void CreateCorpse(World world, Entity victim, Entity killer)
    {
        if (!victim.Has<Position, Inventory>())
            return; // can't create a corpse if we don't know where the victim is
        // TODO: don't do for player ?
        ref var position = ref victim.Get<Position>();
        ref var inventory = ref victim.Get<Inventory>();
        // for the moment, drop items on the floor
        foreach (var item in inventory.Items.ToArray())
        {
            //ContainmentSystem.Move(world, item, victim.Get<Position>().Room);
            ItemMovementSystem.DropItem(victim, position.Room, item);
            MessageSystem.Send(killer, $"{victim.DisplayName} drops {item.DisplayName}.");
        }
    }
}
