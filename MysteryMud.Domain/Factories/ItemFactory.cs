using DefaultEcs;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Factories;

public static class ItemFactory
{
    public static Entity CreateItemInRoom(World world, string name, string description, Entity room)
    {
        var item = world.CreateEntity();
        item.Set(new ItemTag());
        item.Set(new Name { Value = name });
        item.Set(new Level { Value = 1 });
        item.Set(new Description { Value = description });
        item.Set(new ItemEffects
        {
            Data = new EffectsCollection
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
        });
        item.Set(new Location { Room = room });
        // TODO: check that room has RoomContents component
        room.Get<RoomContents>().Items.Add(item);
        return item;
    }

    public static Entity CreateItemInInventory(World world, string name, string description, Entity character)
    {
        var item = world.CreateEntity();
        item.Set(new ItemTag());
        item.Set(new Name { Value = name });
        item.Set(new Level { Value = 1 });
        item.Set(new Description { Value = description });
        item.Set(new ItemEffects
        {
            Data = new EffectsCollection
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
        });
        item.Set(new ContainedIn { Character = character });
        // TODO: check that character has Inventory component
        character.Get<Inventory>().Items.Add(item);
        return item;
    }

    public static Entity CreateItemInContainer(World world, string name, string description, Entity container)
    {
        var item = world.CreateEntity();
        item.Set(new ItemTag());
        item.Set(new Name { Value = name });
        item.Set(new Level { Value = 1 });
        item.Set(new Description { Value = description });
        item.Set(new ItemEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            });
        item.Set(new ContainedIn { Container = container });
        // TODO: check that container has ContainerContents component
        container.Get<ContainerContents>().Items.Add(item);
        return item;
    }
}
