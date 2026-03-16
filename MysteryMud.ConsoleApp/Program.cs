using Arch.Core;
using MysteryMud.ConsoleApp.Commands;
using MysteryMud.ConsoleApp.Components;
using MysteryMud.ConsoleApp.Events;
using MysteryMud.ConsoleApp.Systems;

class Program
{

    static void Main()
    {
        var game = new Game();

        var room1 = game.World.Create(new Room { Title = "Town Square", Description = "A town square." }, new RoomEntities(), new RoomItems());
        var room2 = game.World.Create(new Room { Title = "Temple Square", Description = "The temple square." }, new RoomEntities(), new RoomItems());
        var cave = game.World.Create(new Room { Title = "Dark Cave", Description = "A damp cave with dripping water." }, new DarkRoom(), new RoomEntities(), new RoomItems() );

        game.World.Add(room1, new Exit { Direction = "north", TargetRoom = room2 });
        game.World.Add(room2, new Exit { Direction = "south", TargetRoom = room1 });

        var player = CreatePlayer(game.World, "Hero", room1);
        var goblin = CreateGoblin(game.World, room1);

        // add sword on floor in room1
        var sword = game.World.Create(
            new Item
            {
                Name = "Rusty Sword",
                Description = "An old sword with a chipped blade."
            }
        );
        ref var roomItems = ref game.World.Get<RoomItems>(room1);
        roomItems.Items.Add(sword);

        var torch = game.World.Create(
            new Item
            {
                Name = "Torch",
                Description = "A wooden torch burning brightly."
            },
            new LightSource { Intensity = 1 }
        );
        ref var inv = ref game.World.Get<Inventory>(player);
        inv.Items.Add(torch);

        game.Commands.Commands.Enqueue(new AttackCommand(player, goblin));

        float dt = 1f;

        while (true)
        {
            CommandSystem.Run(game.World, game.Commands);
            AiSystem.Run(game.World);

            BuffSystem.Run(game.World, dt, game.Cmd);
            DotSystem.Run(game.World, dt, game.Cmd);
            StatSystem.Run(game.World);
            CombatSystem.Run(game.World, game.CombatEvents);
            CombatEventSystem.Process(game.World, game.CombatEvents, game.Cmd);
            DeathSystem.Run(game.World, game.Events);

            game.Cmd.Playback(game.World);

            Thread.Sleep(100);
        }
    }

    class Game
    {
        //public World World = World.Create();
        //public CommandBuffer Cmd = new();
        //public CombatEventQueue CombatEvents = new();
        public World World = World.Create();
        public CommandQueue Commands = new();
        public CommandBuffer Cmd = new();
        public CombatEventQueue CombatEvents = new();
        public EventBus Events = new();
    }

    static Entity CreatePlayer(World world, string name, Entity room)
    {
        var e = world.Create(
            new Name { Value = name },
            new PlayerTag(),
            new Inventory(),
            new Equipment(),
            new InRoom { Room = room },
            new BaseStats { Strength = 10, Agility = 10, Vitality = 10 },
            new EffectiveStats(),
            new StatsDirty { Value = true },
            new Health { Current = 100, Max = 100 },
            new Equipment(),
            new Attack { Damage = 5 }
        );

        AddToRoom(world, e, room);
        return e;
    }

    static Entity CreateGoblin(World world, Entity room)
    {
        var e = world.Create(
            new Name { Value = "Goblin" },
            new NpcTag(),
            new Inventory(),
            new Equipment(),
            new InRoom { Room = room },
            new BaseStats { Strength = 7, Agility = 6, Vitality = 8 },
            new EffectiveStats(),
            new StatsDirty { Value = true },
            new Health { Current = 50, Max = 50 },
            new Attack { Damage = 4 }
        );

        AddToRoom(world, e, room);
        return e;
    }

    static void AddToRoom(World world, Entity entity, Entity room)
    {
        ref var list = ref world.Get<RoomEntities>(room);
        list.Entities.Add(entity);
    }
}