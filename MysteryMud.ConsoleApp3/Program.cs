using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3;
using MysteryMud.ConsoleApp3.Commands;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Buff;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Enums;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Network;

CommandRegistry.Register("kill", new KillCommand());
CommandRegistry.Register("get", new GetCommand());
CommandRegistry.Register("look", new LookCommand());
CommandRegistry.Register("say", new SayCommand());
CommandRegistry.Register("tell", new TellCommand());
CommandRegistry.Register("inventory", new InventoryCommand());
CommandRegistry.Register("equipment", new EquipmentCommand());
CommandRegistry.Register("wear", new WearCommand());
CommandRegistry.Register("remove", new RemoveCommand());
CommandRegistry.Register("drop", new DropCommand());
CommandRegistry.Register("give", new GiveCommand());
CommandRegistry.Register("put", new PutCommand());
CommandRegistry.Register("north", new NorthCommand());
CommandRegistry.Register("south", new SouthCommand());
CommandRegistry.Register("destroy", new DestroyCommand());
CommandRegistry.Register("sacrifice", new SacrificeCommand());

var world = World.Create();

//  temple
//    |
//  market
//    |
//  common
var temple = WorldFactory.CreateRoom(world, 1, "temple square", "the temple square");
var market = WorldFactory.CreateRoom(world, 2, "market square", "the market square");
var common = WorldFactory.CreateRoom(world, 3, "common square", "the common square");
WorldFactory.LinkRoom(world, temple, market, Direction.South);
WorldFactory.LinkRoom(world, market, temple, Direction.North);
WorldFactory.LinkRoom(world, market, common, Direction.South);
WorldFactory.LinkRoom(world, common, market, Direction.North);

var player = WorldFactory.CreatePlayer(world, "sinac", market);
var goblin = WorldFactory.CreateMob(world, "goblin", "a goblin", market);
var sword = WorldFactory.CreateItemInRoom(world, "sword", "a %#FFFFFF>#FFFF00shiny sword%x", market);
sword.Add(new Equipable { Slot = EquipmentSlot.MainHand });
var chest = world.Create(
            new Item(),
            new Name { Value = "chest" },
            new Description { Value = "a chest" },
            new Position { Room = market },
            new Container { Capacity = 10 },
            new ContainerContents { Items = new List<Entity>() }
        );
market.Get<RoomContents>().Items.Add(chest);
var gem = WorldFactory.CreateItemInContainer(world, "gem", "a %#FF0000>#FF00FFsparkling gem%x", chest);
var trash = WorldFactory.CreateItemInRoom(world, "trash", "some trash", market);

WorldFactory.StartingRoomEntity = market;
WorldFactory.RespawnRoomEntity = temple;

var telnetServer = new TelnetServer(4000);
//_ = telnetServer.Start(world);
Task.Run(() => telnetServer.Start(world));

//
CommandDispatcher.Dispatch(player, "look".AsSpan());
CommandDispatcher.Dispatch(player, "get all.sword".AsSpan());
CommandDispatcher.Dispatch(player, "look".AsSpan());
CommandDispatcher.Dispatch(player, "inventory".AsSpan());
CommandDispatcher.Dispatch(goblin, "say you stole my sword".AsSpan());
CommandDispatcher.Dispatch(player, "tell goblin you're a liar".AsSpan());
//CommandDispatcher.Dispatch(sword, "inventory".AsSpan()); will crash because sword is not a character and doesn't have inventory component
CommandDispatcher.Dispatch(player, "look chest".AsSpan());
CommandDispatcher.Dispatch(player, "get all from chest".AsSpan());
CommandDispatcher.Dispatch(player, "inventory".AsSpan());
CommandDispatcher.Dispatch(player, "wear gem".AsSpan());
CommandDispatcher.Dispatch(player, "wear sword".AsSpan());
CommandDispatcher.Dispatch(player, "inventory".AsSpan());
CommandDispatcher.Dispatch(player, "equipment".AsSpan());
CommandDispatcher.Dispatch(player, "drop toto".AsSpan());
CommandDispatcher.Dispatch(player, "drop gem".AsSpan());
CommandDispatcher.Dispatch(player, "give sword goblin".AsSpan());
CommandDispatcher.Dispatch(goblin, "wear sword".AsSpan());
CommandDispatcher.Dispatch(goblin, "remove sword".AsSpan());
CommandDispatcher.Dispatch(goblin, "wear sword".AsSpan());
CommandDispatcher.Dispatch(player, "look goblin".AsSpan());
CommandDispatcher.Dispatch(player, "inventory".AsSpan());
CommandDispatcher.Dispatch(player, "get gem".AsSpan());
CommandDispatcher.Dispatch(player, "put gem chest".AsSpan());
CommandDispatcher.Dispatch(player, "look chest".AsSpan());
CommandDispatcher.Dispatch(player, "look".AsSpan());
CommandDispatcher.Dispatch(temple, "look".AsSpan());
CommandDispatcher.Dispatch(chest, "look".AsSpan());
CommandDispatcher.Dispatch(gem, "look".AsSpan());
CommandDispatcher.Dispatch(goblin, "get trash".AsSpan());

CreateStrengthBuff(world, player);
player.Add<DirtyStats>();

//CommandDispatcher.Dispatch(player, "kill goblin".AsSpan());

var gameLoop = new GameLoop();

//for (int i = 0; i < 8; i++)
//{
//    Console.WriteLine("TICK: " + i);

//    gameLoop.Tick(world, i);
//    CommandDispatcher.Dispatch(player, "look".AsSpan());
//}

while (true)
{
    gameLoop.Tick(world, 0);

    Thread.Sleep(100);
}

static Entity CreateStrengthBuff(World world, Entity target)
{
    return world.Create(
        new BuffTag(),
        new BuffTarget { Target = target },
        new Duration { RemainingTicks = 600 },
        new BuffModifiers
        {
            Values = new List<StatModifier>
            {
                new StatModifier
                {
                    Stat = StatType.Strength,
                    Type = ModifierType.Add,
                    Value = 5
                }
            }
        }
    );
}

