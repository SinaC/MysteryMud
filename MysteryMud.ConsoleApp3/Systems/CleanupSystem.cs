using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Mobiles;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems;

public static class CleanupSystem
{
    // TODO check disconnected players and remove them from the world
    // check Position for characters
    // check Position for items
    // check ContainedIn for items
    // check Equipped for items
    public static void Cleanup(World world)
    {
        // destroy disconnected players
        var disconnectedPlayersQuery = new QueryDescription()
                .WithAll<DisconnectedTag>();
        world.Query(disconnectedPlayersQuery, (Entity player, ref DisconnectedTag disconnectedTag) =>
        {
            Logger.Logger.Cleanup.CleanupPlayer(player);

            ref var position = ref player.TryGetRef<Position>(out var hasPosition);
            if (hasPosition)
            {
                ref var roomContents = ref position.Room.Get<RoomContents>();
                roomContents.Characters.Remove(player);
            }

            // TODO: destroy any items the character is carrying or equipped with
            // TODO: if the character is a pet, remove it from its owner's pet list
            // TODO: if the character is a follower, remove it from its leader's follower list

            world.Destroy(player);
        });

        // destroy NPCs
        var destroyCharactersQuery = new QueryDescription()
                .WithAll<Dead, Position, NpcTag>();
        world.Query(destroyCharactersQuery, (Entity character, ref Dead deadTag, ref Position position, ref NpcTag npcTag) =>
        {
            Logger.Logger.Cleanup.CleanupCharacterFromRoom(character, position.Room);

            ref var roomContents = ref position.Room.Get<RoomContents>();
            roomContents.Characters.Remove(character);

            // TODO: destroy any items the character is carrying or equipped with
            // TODO: if the character is a pet, remove it from its owner's pet list
            // TODO: if the character is a follower, remove it from its leader's follower list

            world.Destroy(character);
        });

        // destroy items
        var destroyItemsQuery = new QueryDescription()
                .WithAll<DestroyedTag>()
                .WithAny<Position, ContainedIn, Equipped>();
        //world.Query(destroyItemsQuery, (Entity item, ref DestroyedTag destroyedTag, ref Position position, ref ContainedIn containedIn, ref Equipped equipped) => // doesn't work
        world.Query(destroyItemsQuery, (Entity item, ref DestroyedTag destroyedTag) =>
        {
            // check if the item is on the ground
            if (item.Has<Position>())
            {
                ref var position = ref item.Get<Position>();

                Logger.Logger.Cleanup.CleanupItemFromRoom(item, position.Room);

                ref var roomContents = ref position.Room.Get<RoomContents>();
                roomContents.Items.Remove(item);
            }
            // check if the item is in a container or inventory
            if (item.Has<ContainedIn>())
            {
                ref var containedIn = ref item.Get<ContainedIn>();
                if (containedIn.Character != Entity.Null)
                {
                    Logger.Logger.Cleanup.CleanupItemFromInventory(item, containedIn.Character);

                    ref var inventory = ref containedIn.Character.Get<Inventory>();
                    inventory.Items.Remove(item);
                }
                else if (containedIn.Container != Entity.Null)
                {
                    Logger.Logger.Cleanup.CleanupItemFromContainer(item, containedIn.Container);

                    ref var containerContents = ref containedIn.Container.Get<ContainerContents>();
                    containerContents.Items.Remove(item);
                }
            }
            // check if the item is equipped should never happen)
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                ref var equipment = ref equipped.Wearer.Get<Equipment>();
                foreach (var slot in equipment.Slots.Keys.ToList())
                {
                    if (equipment.Slots[slot] == item)
                    {
                        Logger.Logger.Cleanup.CleanupItemFromEquipment(item, equipped.Wearer, slot);

                        equipment.Slots[slot] = Entity.Null;
                    }
                }
            }
            // finally, destroy the item
            world.Destroy(item);
        });
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