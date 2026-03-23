using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Domain.Components;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using MysteryMud.ConsoleApp3.Domain.Components.Items;
using MysteryMud.ConsoleApp3.Domain.Components.Rooms;
using MysteryMud.ConsoleApp3.Domain.Factories;

namespace MysteryMud.ConsoleApp3.Systems;

public static class DeathSystem
{
    public static void Process(SystemContext ctx, GameState state)
    {
        var query = new QueryDescription()
          .WithAll<Dead>();
        state.World.Query(query, (Entity entity, ref Dead dead) =>
        {
            HandleDeath(ctx, state.World, entity, dead.Killer); // TODO: pass killer
        });
    }

    private static void HandleDeath(SystemContext ctx, World world, Entity victim, Entity killer)
    {
        //TODO: log
        CreateCorpse(ctx, world, victim, killer);

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
            victim.Add(new RespawnState { RespawnRoom = RoomFactory.RespawnRoomEntity });
    }

    private static void RemoveFromRoomContents(World world, Entity victim)
    {
        ref var location = ref victim.TryGetRef<Location>(out var hasLocation);
        if (!hasLocation)
            return; // can't remove from room contents if we don't know where the victim is
        ref var roomContents = ref location.Room.Get<RoomContents>();
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
    private static void CreateCorpse(SystemContext ctx, World world, Entity victim, Entity killer)
    {
        if (!victim.Has<Location, Inventory>())
            return; // can't create a corpse if we don't know where the victim is
        // TODO: don't do for player ?
        ref var location = ref victim.Get<Location>();
        ref var inventory = ref victim.Get<Inventory>();
        // for the moment, drop items on the floor
        foreach (var item in inventory.Items.ToArray())
        {
            // Unequip if necessary
            ref var equipped = ref item.TryGetRef<Equipped>(out var isEquipped);
            if (isEquipped)
            {
                EquipmentSystem.Unequip(victim, equipped.Slot);
            }

            //ContainmentSystem.Move(world, item, victim.Get<Location>().Room);
            ItemMovementSystem.DropItem(victim, location.Room, item);
            ctx.MessageBus.Publish(killer, $"{victim.DisplayName} drops {item.DisplayName}.");
        }
    }
}
