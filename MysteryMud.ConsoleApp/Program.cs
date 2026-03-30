using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Commands;
using MysteryMud.Application.Dispatching;
using MysteryMud.Application.ExplicitCommands;
using MysteryMud.Application.Parsing;
using MysteryMud.ConsoleApp;
using MysteryMud.ConsoleApp.Demo;
using MysteryMud.ConsoleApp.Hosting;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Factories;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence;
using Serilog;

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

// initialize options
var gamePaths = new GamePathsOptions();
configuration.GetSection("GamePaths").Bind(gamePaths);

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
RoomFactory.LinkRoom(world, temple, market, DirectionKind.South);
RoomFactory.LinkRoom(world, market, temple, DirectionKind.North);
RoomFactory.LinkRoom(world, market, common, DirectionKind.South);
RoomFactory.LinkRoom(world, common, market, DirectionKind.North);

var player = PlayerFactory.CreatePlayer(world, "player", market);
var goblin = MobFactory.CreateMob(world, "goblin", "a goblin", market);
var troll = MobFactory.CreateMob(world, "troll", "a troll", market);
troll.Get<Health>().Current = 10000;
troll.Get<Health>().Max = 10000;
var sword = ItemFactory.CreateItemInRoom(world, "sword", "a %#FFFFFF>#FFFF00shiny sword%x", market);
sword.Add(new Equipable { Slot = EquipmentSlotKind.MainHand });
var chest = world.Create(
            new ItemTag(),
            new Name { Value = "chest" },
            new Description { Value = "a chest" },
            new Location { Room = market },
            new Container { Capacity = 10 },
            new ContainerContents { Items = [] }
        );
market.Get<RoomContents>().Items.Add(chest);
var gem = ItemFactory.CreateItemInContainer(world, "gem", "a %#FF0000>#FF00FFsparkling gem%x", chest);
var trash = ItemFactory.CreateItemInRoom(world, "trash", "some trash", market);
var collar = ItemFactory.CreateItemInInventory(world, "collar", "a nice collar", goblin);

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

var basePath = AppContext.BaseDirectory;

// load spell definitions
var spellLoader = new JsonSpellLoader();
var spellDatabase = spellLoader.LoadSpells(Path.Combine(basePath, gamePaths.SpellsJson));

// load command definitions
var commandLoader = new JsonCommandLoader();
var commandDefinitions = commandLoader.Load(Path.Combine(basePath, gamePaths.CommandsJson));

// load social definitions
var socialLoader = new JsonSocialLoader();
var socialDefinitions = socialLoader.Load(Path.Combine(basePath, gamePaths.SocialsJson));

// initialize command registry (Infrastructure)
var commandRegistry = new CommandRegistry();
var explicitCommands = new List<ICommand>();
// help command
var helpCommand = new HelpCommand(commandRegistry); // special case for help command since it needs registry reference
explicitCommands.Add(helpCommand);
// socials command (display all socials)
var socialsCommand = new SocialsCommand(commandRegistry);
explicitCommands.Add(socialsCommand);
// social commands (one by social definition)
foreach (var socialDefinition in socialDefinitions)
{
    var socialCommand = new SocialCommand(socialDefinition);
    explicitCommands.Add(socialCommand);
}
// commands from assemblies
commandRegistry.RegisterCommands(commandDefinitions, [typeof(TestCommand).Assembly], explicitCommands);

// initialize dispatcher and parser (Application)
var commandParser = new CommandParser();
var commandDispatcher = new CommandDispatcher(commandRegistry, commandParser);


// run demo
//Demo.Run(logger, world, commandDispatcher);
Demo2.Run(logger, world, commandDispatcher);

// start game server
var gameServer = new GameServer(logger, world, commandDispatcher);
gameServer.Start();