using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Dispatching;
using MysteryMud.Application.ExplicitCommands;
using MysteryMud.Application.Registry;
using MysteryMud.ConsoleApp;
using MysteryMud.ConsoleApp.Hosting;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Ability.Resolvers;
using MysteryMud.Domain.Action.Attack;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Factories;
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
    .Enrich.FromLogContext()
    .MinimumLevel.Verbose()
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

var player = PlayerFactory.CreateAdmin(world, "player", market);
var goblin = MobFactory.CreateMob(world, "goblin", "a goblin", market);
var troll = MobFactory.CreateMob(world, "troll", "a troll", market);
troll.Get<Health>().Current = 10000;
troll.Get<Health>().Max = 10000;
troll.Get<BaseHealth>().Max = 10000;
ref var trollEffectiveStats = ref troll.Get<EffectiveStats>();
trollEffectiveStats.Dodge = 0; // for testing, make sure all hits land so we can see the counterattack in action
trollEffectiveStats.Parry = 0; // for testing, make sure all hits land so we can see the counterattack in action
trollEffectiveStats.CounterAttack = 100; // for testing, make sure all we counterattack every time so we can see the counterattack in action
var sword = ItemFactory.CreateItemInRoom(world, "sword", "a %#FFFFFF>#FFFF00shiny sword%x", market);
sword.Add(new Equipable { Slot = EquipmentSlotKind.MainHand });
sword.Add(new Weapon { Kind = WeaponKind.Sword, DiceCount = 5, DiceValue = 10, ProcIds = ["Flaming".ComputeUniqueId()] }); // add flaming
var chest = world.Create(
            new ItemTag(),
            new Name { Value = "chest" },
            new Description { Value = "a chest" },
            new Location { Room = market },
            new Container { Capacity = 10 },
            new ContainerContents { Items = [] }
        );
var dagger = ItemFactory.CreateItemInRoom(world, "dagger", "a vampiric dagger", market);
dagger.Add(new Equipable { Slot = EquipmentSlotKind.MainHand });
dagger.Add(new Weapon { Kind = WeaponKind.Dagger, DiceCount = 5, DiceValue = 8, ProcIds = ["Vampiric".ComputeUniqueId()] }); // add vampiric
dagger.Get<Level>().Value = 20;
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

// load effect definitions
var effectLoader = new JsonEffectLoader();
var effectDefinitions = effectLoader.Load(Path.Combine(basePath, gamePaths.EffectsJson));
var effectActionFactory = new EffectActionFactory(logger);
var effectRuntimeFactory = new EffectRuntimeFactory(effectActionFactory);
var effectRegistry = new EffectRegistry(effectRuntimeFactory);
effectRegistry.RegisterEffects(effectDefinitions);

// define ability outcome resolver registry
// TODO: autodiscover with reflection
var abilityOutcomeResolverRegistry = new AbilityOutcomeResolverRegistry();
abilityOutcomeResolverRegistry.Register("default", new DefaultOutcomeResolver());
abilityOutcomeResolverRegistry.Register("chancebased", new ChanceBasedOutcomeResolver());
abilityOutcomeResolverRegistry.Register("berserk", new BerserkOutcomeResolver());

// load ability definitions
var abilityLoader = new JsonAbilityLoader();
var abilityDefinitions = abilityLoader.Load(Path.Combine(basePath, gamePaths.AbilitiesJson));
var abilityRegistry = new AbilityRegistry(effectRegistry, abilityOutcomeResolverRegistry);
abilityRegistry.Register(abilityDefinitions);

// load weapon proc definitions
var weaponProcLoader = new JsonWeaponProcLoader();
var weaponProcDefinitions = weaponProcLoader.Load(Path.Combine(basePath, gamePaths.WeaponProcsJson));
var weaponProcRegistry = new WeaponProcRegistry(effectRegistry);
weaponProcRegistry.Register(weaponProcDefinitions);

// load command definitions
var commandLoader = new JsonCommandLoader();
var commandDefinitions = commandLoader.Load(Path.Combine(basePath, gamePaths.CommandsJson));

// initialize command registry (Infrastructure)
var commandRegistry = new CommandRegistry(logger);
var explicitCommands = new List<IExplicitCommand>
{
    new HelpCommand(commandRegistry),
    new SocialsCommand(commandRegistry),
    new ForceCommand(logger, commandRegistry),
    new TestCommand(effectRegistry),
    new CastCommand(logger, abilityRegistry)
};

// social commands (one by social definition)
var socialLoader = new JsonSocialLoader();
var socialDefinitions = socialLoader.Load(Path.Combine(basePath, gamePaths.SocialsJson));
foreach (var socialDefinition in socialDefinitions)
{
    var socialCommand = new SocialCommand(logger, socialDefinition);
    explicitCommands.Add(socialCommand);
}
// skill commands (from abilities with type Skill)
var skillCommandDefinitions = abilityDefinitions.Where(x => x.Kind == AbilityKind.Skill && x.Command is not null).Select(x => x.Command!.Value).ToArray();
foreach (var skillCommandDefinition in skillCommandDefinitions)
{
    var skillCommand = new SkillCommand(logger, abilityRegistry, skillCommandDefinition);
    explicitCommands.Add(skillCommand);
}

// commands from assemblies
commandRegistry.RegisterCommands(commandDefinitions, [typeof(TestCommand).Assembly], explicitCommands);

// initialize dispatcher and parser (Application)
var commandDispatcher = new CommandDispatcher(commandRegistry);

// run demo
//Demo.Run(logger, world, commandDispatcher);
//Demo2.Run(logger, world, commandDispatcher, effectRegistry);

// start game server
var gameServer = new GameServer(logger, world, commandDispatcher, effectRegistry, abilityRegistry, abilityOutcomeResolverRegistry, weaponProcRegistry);
gameServer.Start();