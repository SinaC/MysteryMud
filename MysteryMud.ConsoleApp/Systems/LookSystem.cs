using Arch.Core;
using MysteryMud.ConsoleApp.Components;

namespace MysteryMud.ConsoleApp.Systems;

static class LookSystem
{
    public static void Execute(World world, Entity actor)
    {
        if (!world.Has<InRoom>(actor))
            return;

        var roomEntity = world.Get<InRoom>(actor).Room;
        var room = world.Get<Room>(roomEntity);

        Console.WriteLine();
        Console.WriteLine(room.Title);
        Console.WriteLine(room.Description);
        Console.WriteLine();
        ShowExits(world, roomEntity);
        ShowItems(world, roomEntity);
        ShowCharacters(world, roomEntity, actor);
    }

    static void ShowItems(World world, Entity room)
    {
        var items = world.Get<RoomItems>(room);

        if (items.Items.Count == 0)
            return;

        Console.WriteLine("You see:");

        foreach (var itemEntity in items.Items)
        {
            var item = world.Get<Item>(itemEntity);
            Console.WriteLine($"  {item.Name}");
        }

        Console.WriteLine();
    }

    static void ShowCharacters(World world, Entity room, Entity viewer)
    {
        var entities = world.Get<RoomEntities>(room).Entities;

        foreach (var entity in entities)
        {
            if (entity == viewer)
                continue;

            if (!world.Has<Name>(entity))
                continue;

            if (!Visibility.CanSee(world, viewer, entity))
                continue;

            var name = world.Get<Name>(entity).Value;

            if (world.Has<PlayerTag>(entity))
                Console.WriteLine($"{name} is standing here.");

            else if (world.Has<NpcTag>(entity))
                Console.WriteLine($"A {name} is here.");
        }
    }

    static void ShowExits(World world, Entity room)
    {
        var exits = new List<string>();

        var query = new QueryDescription()
            .WithAll<Exit>();
        world.Query(query, (Entity e, ref Exit exit) =>
        {
                if (e == room)
                    exits.Add(exit.Direction);
            });

        if (exits.Count > 0)
            Console.WriteLine($"Exits: {string.Join(", ", exits)}");
    }
}
