using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Factories;

public static class ItemFactory
{
    public static EntityId CreateItemInRoom(World world, string name, string description, EntityId room)
    {
        var item = world.CreateEntity(
            new ItemTag(),
            new Name { Value = name },
            new Level { Value = 1 },
            new Description { Value = description },
            new ItemEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<EntityId>?[32]
                },
            },
            new Location { Room = room }
        );
        // TODO: check that room has RoomContents component
        world.Get<RoomContents>(room).Items.Add(item);
        return item;
    }

    public static EntityId CreateItemInInventory(World world, string name, string description, EntityId character)
    {
        var item = world.CreateEntity(
            new ItemTag(),
            new Name { Value = name },
            new Level { Value = 1 },
            new Description { Value = description },
            new ItemEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<EntityId>?[32]
                },
            },
            new ContainedIn { Character = character }
        );
        // TODO: check that character has Inventory component
        world.Get<Inventory>(character).Items.Add(item);
        return item;
    }

    public static EntityId CreateItemInContainer(World world, string name, string description, EntityId container)
    {
        var item = world.CreateEntity(
            new ItemTag(),
            new Name { Value = name },
            new Level { Value = 1 },
            new Description { Value = description },
            new ItemEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<EntityId>?[32]
                },
            },
            new ContainedIn { Container = container }
        );
        // TODO: check that container has ContainerContents component
        world.Get<ContainerContents>(container).Items.Add(item);
        return item;
    }
}
