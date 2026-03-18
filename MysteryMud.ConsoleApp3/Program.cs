using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3;
using MysteryMud.ConsoleApp3.Commands;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Network;
using MysteryMud.ConsoleApp3.Persistance;
using MysteryMud.ConsoleApp3.Systems;
using System.Text.Json;

// load spells
var spellJson = @"
{
  ""Effects"": [
    {
      ""Name"": ""Poison"",
      ""Tag"": ""Poison"",
      ""Stacking"": ""Stack"",
      ""MaxStacks"": 3,
      ""StatModifiers"": [
        { ""Stat"": ""Strength"", ""Type"": ""Flat"", ""Value"": -3 }
      ],
      ""Flags"": [""Poison""],
      ""DurationFormula"": ""4 * caster.Strength"",
      ""Dot"": {
         ""DamageFormula"": ""1"",
         ""DamageType"": ""Poison"",
         ""TickRate"": 2
      }
    },
    {
      ""Name"": ""Bless"",
      ""Tag"": ""Bless"",
      ""Stacking"": ""Refresh"",
      ""MaxStacks"": 1,
      ""StatModifiers"": [
        { ""Stat"": ""HitRoll"", ""Type"": ""Flat"", ""Value"": 5 },
        { ""Stat"": ""Strength"", ""Type"": ""AddPercent"", ""Value"": 10 }
      ],
      ""Flags"": [""Bless""],
      ""DurationFormula"": ""60"",
      ""Hot"": {
         ""HealFormula"": ""3 + caster.Level / 2"",
         ""TickRate"": 3
      }
    }
  ],
  ""Spells"": [
    {
      ""Name"": ""a"",
      ""Effects"": [""Poison""]
    },
    {
      ""Name"": ""b"",
      ""Effects"": [""Bless""]
    }
  ]
}
";

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var doc = JsonSerializer.Deserialize<SpellJsonRoot>(spellJson, options);

// convert to spell database
var spellDatabase = SpellLoader.LoadSpells(doc);
SpellSystem.SpellDatabase = spellDatabase;

// register commands
CommandRegistry.Register("test", new TestCommand());
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
CommandRegistry.Register("mstat", new MstatCommand());
CommandRegistry.Register("cast", new CastCommand());

// create world
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
var troll = WorldFactory.CreateMob(world, "troll", "a troll", market);
troll.Get<Health>().Current = 10000;
troll.Get<Health>().Max = 10000;
var sword = WorldFactory.CreateItemInRoom(world, "sword", "a %#FFFFFF>#FFFF00shiny sword%x", market);
sword.Add(new Equipable { Slot = EquipmentSlot.MainHand });
var chest = world.Create(
            new Item(),
            new Name { Value = "chest" },
            new Description { Value = "a chest" },
            new Position { Room = market },
            new Container { Capacity = 10 },
            new ContainerContents { Items = [] }
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
CommandDispatcher.Dispatch(world, player, "look".AsSpan());
CommandDispatcher.Dispatch(world, player, "get all.sword".AsSpan());
CommandDispatcher.Dispatch(world, player, "look".AsSpan());
CommandDispatcher.Dispatch(world, player, "inventory".AsSpan());
CommandDispatcher.Dispatch(world, goblin, "say you stole my sword".AsSpan());
CommandDispatcher.Dispatch(world, player, "tell goblin you're a liar".AsSpan());
//CommandDispatcher.Dispatcworld, h(sword, "inventory".AsSpan()); will crash because sword is not a character and doesn't have inventory component
CommandDispatcher.Dispatch(world, player, "look chest".AsSpan());
CommandDispatcher.Dispatch(world, player, "get all from chest".AsSpan());
CommandDispatcher.Dispatch(world, player, "inventory".AsSpan());
CommandDispatcher.Dispatch(world, player, "wear gem".AsSpan());
CommandDispatcher.Dispatch(world, player, "wear sword".AsSpan());
CommandDispatcher.Dispatch(world, player, "inventory".AsSpan());
CommandDispatcher.Dispatch(world, player, "equipment".AsSpan());
CommandDispatcher.Dispatch(world, player, "drop toto".AsSpan());
CommandDispatcher.Dispatch(world, player, "drop gem".AsSpan());
CommandDispatcher.Dispatch(world, player, "give sword goblin".AsSpan());
CommandDispatcher.Dispatch(world, goblin, "wear sword".AsSpan());
CommandDispatcher.Dispatch(world, goblin, "remove sword".AsSpan());
CommandDispatcher.Dispatch(world, goblin, "wear sword".AsSpan());
CommandDispatcher.Dispatch(world, player, "look goblin".AsSpan());
CommandDispatcher.Dispatch(world, player, "inventory".AsSpan());
CommandDispatcher.Dispatch(world, player, "get gem".AsSpan());
CommandDispatcher.Dispatch(world, player, "put gem chest".AsSpan());
CommandDispatcher.Dispatch(world, player, "look chest".AsSpan());
CommandDispatcher.Dispatch(world, player, "look".AsSpan());
CommandDispatcher.Dispatch(world, temple, "look".AsSpan());
CommandDispatcher.Dispatch(world, chest, "look".AsSpan());
CommandDispatcher.Dispatch(world, gem, "look".AsSpan());
CommandDispatcher.Dispatch(world, goblin, "get trash".AsSpan());

// testing combat
//CommandDispatcher.Dispatch(player, "kill goblin".AsSpan());

// testing buffs and dots
CommandDispatcher.Dispatch(world, goblin, "test troll poison".AsSpan());
//CommandDispatcher.Dispatch(world, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
//CommandDispatcher.Dispatch(world, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
//CommandDispatcher.Dispatch(world, player, "test troll bless".AsSpan()); // will not be applied because StackingRule is None
//CommandDispatcher.Dispatch(world, player, "test troll bless".AsSpan());
//
//CommandDispatcher.Dispatch(world, goblin, "test troll poison".AsSpan());
//CommandDispatcher.Dispatch(world, player, "test troll poison".AsSpan());
//CommandDispatcher.Dispatch(world, goblin, "test troll poison".AsSpan());

//// tick 1
//GameLoop.Tick(world);
//// tick 2
//GameLoop.Tick(world);
//// tick 3
//GameLoop.Tick(world);
//// tick 4
//GameLoop.Tick(world);
//// tick 5
//GameLoop.Tick(world);
//// tick 6
//GameLoop.Tick(world);
//// tick 7
//GameLoop.Tick(world);
//Console.WriteLine("Press any key to exit...");
//Console.ReadLine();

while (true)
{
    GameLoop.Tick(world);

    Thread.Sleep(100);
}

