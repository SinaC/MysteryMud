using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Commands;
using MysteryMud.Application.Commands.Registry;
using MysteryMud.Application.Systems;
using MysteryMud.ConsoleApp;
using MysteryMud.ConsoleApp.Hosting;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Data.Enums;
using MysteryMud.Domain.Factories;
using MysteryMud.Infrastructure.Persistence;
using MysteryMud.Infrastructure.Persistence.Dto;
using Serilog;
using System.Text.Json;

// build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

// initialize logger
Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Debug() // Set the minimum level
  .ReadFrom.Configuration(configuration)
  .CreateLogger();
var factory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(); // Use Serilog as provider
});

var logger = factory.CreateLogger("MysteryMud");
logger.LogInformation("Log initialized");

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

// create world
var world = World.Create();

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

var player = PlayerFactory.CreatePlayer(world, "player", market);
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

// run demo
Demo.Run(logger, world);

// start game server
var gameServer = new GameServer(logger, world);
gameServer.Start();