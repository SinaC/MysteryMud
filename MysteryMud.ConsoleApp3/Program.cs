using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using MysteryMud.ConsoleApp3;
using MysteryMud.ConsoleApp3.Commands;
using MysteryMud.ConsoleApp3.Commands.Registry;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Infrastructure.Persistence;
using MysteryMud.ConsoleApp3.Infrastructure.Persistence.Dto;
using MysteryMud.ConsoleApp3.Logger;
using MysteryMud.ConsoleApp3.Systems;
using System.Text.Json;

// build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

// initialize logger
Logger.Initialize(configuration);

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
var doc = JsonSerializer.Deserialize<SpellRootData>(spellJson, options);

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
var gameState = new GameState { World = world, CurrentTick = 0 };

//  temple
//    |
//  market
//    |
//  common
var temple = RoomFactory.CreateRoom(world, 1, "temple square", "the temple square");
var market = RoomFactory.CreateRoom(world, 2, "market square", "the market square");
var common = RoomFactory.CreateRoom(world, 3, "common square", "the common square");
RoomFactory.LinkRoom(world, temple, market, Direction.South);
RoomFactory.LinkRoom(world, market, temple, Direction.North);
RoomFactory.LinkRoom(world, market, common, Direction.South);
RoomFactory.LinkRoom(world, common, market, Direction.North);

var player = PlayerFactory.CreatePlayer(world, "sinac", market);
var goblin = MobFactory.CreateMob(world, "goblin", "a goblin", market);
var troll = MobFactory.CreateMob(world, "troll", "a troll", market);
troll.Get<Health>().Current = 10000;
troll.Get<Health>().Max = 10000;
var sword = ItemFactory.CreateItemInRoom(world, "sword", "a %#FFFFFF>#FFFF00shiny sword%x", market);
sword.Add(new Equipable { Slot = EquipmentSlot.MainHand });
var chest = world.Create(
            new Item(),
            new Name { Value = "chest" },
            new Description { Value = "a chest" },
            new Location { Room = market },
            new Container { Capacity = 10 },
            new ContainerContents { Items = [] }
        );
market.Get<RoomContents>().Items.Add(chest);
var gem = ItemFactory.CreateItemInContainer(world, "gem", "a %#FF0000>#FF00FFsparkling gem%x", chest);
var trash = ItemFactory.CreateItemInRoom(world, "trash", "some trash", market);

RoomFactory.StartingRoomEntity = market;
RoomFactory.RespawnRoomEntity = temple;

//var compiler = new FormulaCompiler();
////var formula = "range(1, range( 5, 10)) + 5 * (caster.level + target.level)";
////var formula = "max(1, 2, range(1,5), caster.level, if(caster.strength>caster.level && caster.level != target.level, caster.strength, caster.level))";
//var formula = "max(1, 2, range(1,5), caster.level, if(caster.level >= target.level && sum(1,2,3) < 10 || dice(2,6) == 12, -caster.strength, caster.level))";
////caster.level > target.level && sum(1,2,3) < 10 || dice(2,6) == 12
//var func = compiler.Compile(formula);
//int result = func(null!, troll, player); // result is random between 6 and 55
//Console.WriteLine(result);

////
//CommandDispatcher.Dispatch(gameState, player, "look".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "get all.sword".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "look".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "inventory".AsSpan());
//CommandDispatcher.Dispatch(gameState, goblin, "say you stole my sword".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "tell goblin you're a liar".AsSpan());
////CommandDispatcher.DispatcgameState, h(sword, "inventory".AsSpan()); will crash because sword is not a character and doesn't have inventory component
//CommandDispatcher.Dispatch(gameState, player, "look chest".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "get all from chest".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "inventory".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "wear gem".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "wear sword".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "inventory".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "equipment".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "drop toto".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "drop gem".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "give sword goblin".AsSpan());
//CommandDispatcher.Dispatch(gameState, goblin, "wear sword".AsSpan());
//CommandDispatcher.Dispatch(gameState, goblin, "remove sword".AsSpan());
//CommandDispatcher.Dispatch(gameState, goblin, "wear sword".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "look goblin".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "inventory".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "get gem".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "put gem chest".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "look chest".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "look".AsSpan());
//CommandDispatcher.Dispatch(gameState, temple, "look".AsSpan());
//CommandDispatcher.Dispatch(gameState, chest, "look".AsSpan());
//CommandDispatcher.Dispatch(gameState, gem, "look".AsSpan());
//CommandDispatcher.Dispatch(gameState, goblin, "get trash".AsSpan());

// testing combat
//CommandDispatcher.Dispatch(player, "kill goblin".AsSpan());

// testing buffs and dots
//CommandDispatcher.Dispatch(gameState, goblin, "test troll poison".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
//CommandDispatcher.Dispatch(gameState, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
//CommandDispatcher.Dispatch(gameState, player, "test troll bless".AsSpan()); // will not be applied because StackingRule is None
//CommandDispatcher.Dispatch(gameState, player, "test troll bless".AsSpan());
//
//CommandDispatcher.Dispatch(gameState, goblin, "test troll poison".AsSpan());
//CommandDispatcher.Dispatch(gameState, player, "test troll poison".AsSpan());
//CommandDispatcher.Dispatch(gameState, goblin, "test troll poison".AsSpan());

var gameServer = new GameServer(world);
gameServer.Start();