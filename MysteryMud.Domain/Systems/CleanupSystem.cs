using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Effect.Factories;
using MysteryMud.Domain.Extensions;

namespace MysteryMud.Domain.Systems;

public class CleanupSystem
{
    private readonly ILogger _logger;
    private readonly EffectFactory _effectFactory;

    public CleanupSystem(ILogger logger, EffectFactory effectFactory)
    {
        _logger = logger;
        _effectFactory = effectFactory;
    }

    // check disconnected players and remove them from the world
    // check Location for characters
    // check Location for items
    // check ContainedIn for items
    // check Equipped for items
    public void Tick(GameState state)
    {
        // destroy expired effects
        var expiredEffectsQuery = new QueryDescription()
            .WithAll<ExpiredTag>();
        state.World.Query(expiredEffectsQuery, (Entity effect, ref ExpiredTag expiredTag) =>
        {
            _logger.LogInformation(LogEvents.Cleanup, "Cleaning up expired effect {effectName}", effect.DebugName);

            // remove the effect from the target's CharacterEffects
            _effectFactory.RemoveEffect(state, effect);
        });

        // destroy disconnected players
        var disconnectedPlayersQuery = new QueryDescription()
                .WithAll<DisconnectedTag>();
        state.World.Query(disconnectedPlayersQuery, (Entity player, ref DisconnectedTag disconnectedTag) =>
        {
            _logger.LogInformation(LogEvents.Cleanup,"Cleaning up disconnected player {characterName}", player.DebugName);

            RemoveFromCombat(state.World, player);
            RemoveFromRoomContents(player);
            RemoveFromThreatTable(state.World, player);
            RemoveEffects(state.World, player);

            // TODO: destroy any items the character is carrying or equipped with
            // TODO: if the character is a pet, remove it from its owner's pet list
            // TODO: if the character is a follower, remove it from its leader's follower list

            state.World.Destroy(player);
        });

        // destroy NPCs
        var destroyNpcsQuery = new QueryDescription()
                .WithAll<Dead, Location, NpcTag>();
        state.World.Query(destroyNpcsQuery, (Entity npc, ref Dead deadTag, ref Location location, ref NpcTag npcTag) =>
        {
            _logger.LogInformation(LogEvents.Cleanup,"Cleaning up npc {characterName} from room {roomName}", npc.DebugName, location.Room.DebugName);

            RemoveFromCombat(state.World, npc);
            RemoveFromRoomContents(npc);
            RemoveFromThreatTable(state.World, npc);
            RemoveEffects(state.World, npc);

            // TODO: destroy any items the character is carrying or equipped with
            // TODO: if the character is a pet, remove it from its owner's pet list
            // TODO: if the character is a follower, remove it from its leader's follower list

            state.World.Destroy(npc);
        });

        // destroy items
        var destroyItemsQuery = new QueryDescription()
                .WithAll<DestroyedTag>()
                .WithAny<Location, ContainedIn, Equipped>();
        //world.Query(destroyItemsQuery, (Entity item, ref DestroyedTag destroyedTag, ref Location location, ref ContainedIn containedIn, ref Equipped equipped) => // doesn't work
        state.World.Query(destroyItemsQuery, (Entity item, ref DestroyedTag destroyedTag) =>
        {
            // check if the item is on the ground
            ref var location = ref item.TryGetRef<Location>(out var hasLocation);
            if (hasLocation)
            {
                _logger.LogInformation(LogEvents.Cleanup,"Cleaning up item {itemName} from location {locationName}", item.DebugName, location.Room.DebugName);

                ref var roomContents = ref location.Room.Get<RoomContents>();
                roomContents.Items.Remove(item);
            }
            // check if the item is in a container or inventory
            ref var containedIn = ref item.TryGetRef<ContainedIn>(out var hasContainedIn);
            if (hasContainedIn)
            {
                if (containedIn.Character != Entity.Null)
                {
                    _logger.LogInformation(LogEvents.Cleanup,"Cleaning up item {itemName} from inventory of {inventoryOwnerName}", item.DebugName, containedIn.Character.DebugName);

                    ref var inventory = ref containedIn.Character.Get<Inventory>();
                    inventory.Items.Remove(item);
                }
                else if (containedIn.Container != Entity.Null)
                {
                    _logger.LogInformation(LogEvents.Cleanup,"Cleaning up item {itemName} from container {containerName}", item.DebugName, containedIn.Container.DebugName);

                    ref var containerContents = ref containedIn.Container.Get<ContainerContents>();
                    containerContents.Items.Remove(item);
                }
            }
            // check if the item is equipped should never happen)
            ref var equipped = ref item.TryGetRef<Equipped>(out var isEquipped);
            if (isEquipped)
            {
                ref var equipment = ref equipped.Wearer.Get<Equipment>();
                foreach (var slot in equipment.Slots.Keys.ToList())
                {
                    if (equipment.Slots[slot] == item)
                    {
                        _logger.LogInformation(LogEvents.Cleanup,"Cleaning up item {itemName} from equipment of {wearerName} in slot {slot}", item.DebugName, equipped.Wearer.DebugName, slot);

                        equipment.Slots[slot] = Entity.Null;
                    }
                }
            }
            // finally, destroy the item
            state.World.Destroy(item);
        });
    }

    private static void RemoveFromRoomContents(Entity victim)
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

    private void RemoveFromThreatTable(World world, Entity character) // TODO: optimize, this will loop on every NPC
    {
        var query = new QueryDescription()
            .WithAll<ThreatTable, ActiveThreatTag>();
        world.Query(query, (Entity actor, ref ThreatTable threatTable) =>
        {
            threatTable.Threat.Remove(character);
        });
    }

    private static void RemoveEffects(World world, Entity victim)
    {
        // remove all effects on victim
        ref var characterEffects = ref victim.Get<CharacterEffects>();
        foreach (var effect in characterEffects.Effects)
            world.Destroy(effect);
    }

    public static void FullCleanup(World world)
    {
        // destroy all entities with DeadTag or DestroyedTag in a single query
        var destroyQuery = new QueryDescription()
                .WithAny<Dead, DestroyedTag>();
        world.Query(destroyQuery, world.Destroy);

        // now remove all references to destroyed entities from inventories, rooms, and containers
        var roomContentsQuery = new QueryDescription()
            .WithAll<RoomContents>();
        world.Query(roomContentsQuery, (Entity entity, ref RoomContents roomContents) =>
        {
            roomContents.Characters.RemoveAll(item => !world.IsAlive(item));
            roomContents.Items.RemoveAll(item => !item.IsAlive());
        });

        var containerQuery = new QueryDescription()
                .WithAll<ContainerContents>();
        world.Query(containerQuery, (Entity entity, ref ContainerContents containerContents) =>
        {
            containerContents.Items.RemoveAll(item => !item.IsAlive());
        });

        var inventoryQuery = new QueryDescription()
                .WithAll<Inventory>();
        world.Query(inventoryQuery, (Entity entity, ref Inventory inventory) =>
        {
            inventory.Items.RemoveAll(item => !item.IsAlive());
        });

        var equipmentQuery = new QueryDescription()
            .WithAll<Equipment>();
        world.Query(inventoryQuery, (Entity entity, ref Equipment equipment) =>
        {
            foreach (var slot in equipment.Slots.Keys.ToList())
            {
                if (!world.IsAlive(equipment.Slots[slot]))
                {
                    equipment.Slots[slot] = Entity.Null;
                }
            }
        });
    }
}