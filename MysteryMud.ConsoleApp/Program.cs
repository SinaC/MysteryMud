using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Commands;
using MysteryMud.Application.Dispatching;
using MysteryMud.Application.ExplicitCommands;
using MysteryMud.Application.Registry;
using MysteryMud.Application.Services;
using MysteryMud.ConsoleApp;
using MysteryMud.ConsoleApp.Hosting;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Effects;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Extensions;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Scheduler;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Ability.Resolvers;
using MysteryMud.Domain.Ability.Services;
using MysteryMud.Domain.Action;
using MysteryMud.Domain.Action.Attack;
using MysteryMud.Domain.Action.Attack.Factories;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Factories;
using MysteryMud.Domain.Action.Heal;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Eventing;
using MysteryMud.Infrastructure.Intent;
using MysteryMud.Infrastructure.Network;
using MysteryMud.Infrastructure.Persistence;
using MysteryMud.Infrastructure.Scheduler;
using MysteryMud.Infrastructure.Services;
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


// === Data bootstrap ===
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
effectRegistry.Register(effectDefinitions);

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

//// initialize command registry (Infrastructure)
//var commandRegistry = new CommandRegistry(logger);
//var explicitCommands = new List<IExplicitCommand>
//{
//    new HelpCommand(commandRegistry),
//    new SocialsCommand(commandRegistry),
//    new ForceCommand(logger, commandRegistry),
//    new TestCommand(effectRegistry),
//    new CastCommand(logger, abilityRegistry)
//};

//// social commands (one by social definition)
var socialLoader = new JsonSocialLoader();
var socialDefinitions = socialLoader.Load(Path.Combine(basePath, gamePaths.SocialsJson));
//foreach (var socialDefinition in socialDefinitions)
//{
//    var socialCommand = new SocialCommand(logger, socialDefinition);
//    explicitCommands.Add(socialCommand);
//}
//// skill commands (from abilities with type Skill)
var skillCommandDefinitions = abilityDefinitions.Where(x => x.Kind == AbilityKind.Skill && x.Command is not null).Select(x => x.Command!.Value).ToArray();
//foreach (var skillCommandDefinition in skillCommandDefinitions)
//{
//    var skillCommand = new SkillCommand(logger, abilityRegistry, skillCommandDefinition);
//    explicitCommands.Add(skillCommand);
//}

//// commands from assemblies
//commandRegistry.RegisterCommands(commandDefinitions, [typeof(TestCommand).Assembly], explicitCommands);

//// initialize dispatcher and parser (Application)
//var commandDispatcher = new CommandDispatcher(commandRegistry);

// run demo
//Demo.Run(logger, world, commandDispatcher);
//Demo2.Run(logger, world, commandDispatcher, effectRegistry);

// === DI container ===
var services = new ServiceCollection();

// Infrastructure / primitives
services.AddSingleton(world);
services.AddSingleton(factory.CreateLogger("MysteryMud")); // ILogger

// Register all ICommand implementations for DI resolution
var commandAssembly = typeof(MstatCommand).Assembly;
foreach (var type in commandAssembly.GetTypes()
    .Where(t => typeof(ICommand).IsAssignableFrom(t)
                && !typeof(IExplicitCommand).IsAssignableFrom(t) // exclude explicit commands
                && !t.IsAbstract
                && !t.IsInterface))
{
    services.AddSingleton(type);
}

// RegisterCommands resolves all IExplicitCommand registrations
services.AddSingleton<CommandRegistry>();
services.AddSingleton<ICommandRegistry>(sp =>
{
    var registry = sp.GetRequiredService<CommandRegistry>();

    // Commands that depend on ICommandRegistry must be constructed here,
    // after the registry exists, to break the cycle
    var explicitCommands = new List<IExplicitCommand>
    {
        new HelpCommand(registry),
        new SocialsCommand(registry),
        new ForceCommand(logger, registry),
        new TestCommand(sp.GetRequiredService<IEffectRegistry>()),
        new CastCommand(logger, sp.GetRequiredService<IAbilityRegistry>()),
    };

    // Social commands — data-driven
    foreach (var socialDefinition in socialDefinitions)
        explicitCommands.Add(new SocialCommand(logger, socialDefinition));

    // Skill commands — data-driven
    foreach (var skillCommandDefinition in skillCommandDefinitions)
        explicitCommands.Add(new SkillCommand(logger, sp.GetRequiredService<IAbilityRegistry>(), skillCommandDefinition));

    // Register all IExplicitCommand implementations
    foreach(var explicitCommand in explicitCommands)
        services.AddSingleton<IExplicitCommand>(explicitCommand);

    registry.RegisterCommands(commandDefinitions, [typeof(MstatCommand).Assembly], explicitCommands);
    return registry;
});

// Pre-built registries (instances, not types — already constructed above)
services.AddSingleton<IEffectRegistry>(effectRegistry);
services.AddSingleton<IAbilityRegistry>(abilityRegistry);
services.AddSingleton<IAbilityOutcomeResolverRegistry>(abilityOutcomeResolverRegistry);
services.AddSingleton<IWeaponProcRegistry>(weaponProcRegistry);

// Event buffers
services.AddSingleton<EventBufferRegistry>();
// Each IEventBuffer<T> resolves to its slot in the registry
services.AddSingleton<IEventBuffer<FleeBlockedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().FleeBlocked);
services.AddSingleton<IEventBuffer<MovedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().Moved);
services.AddSingleton<IEventBuffer<ItemGotEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemGot);
services.AddSingleton<IEventBuffer<ItemDroppedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemDropped);
services.AddSingleton<IEventBuffer<ItemGivenEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemGiven);
services.AddSingleton<IEventBuffer<ItemPutEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemPut);
services.AddSingleton<IEventBuffer<ItemWornEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemWorn);
services.AddSingleton<IEventBuffer<ItemRemovedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemRemoved);
services.AddSingleton<IEventBuffer<ItemDestroyedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemDestroyed);
services.AddSingleton<IEventBuffer<ItemSacrificiedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemSacrificed);
services.AddSingleton<IEventBuffer<DamagedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().Damaged);
services.AddSingleton<IEventBuffer<HealedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().Healed);
services.AddSingleton<IEventBuffer<DeathEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().Death);
services.AddSingleton<IEventBuffer<ItemLootedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ItemLooted);
services.AddSingleton<IEventBuffer<LookedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().Looked);
services.AddSingleton<IEventBuffer<TriggeredScheduledEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().TriggeredScheduled);
services.AddSingleton<IEventBuffer<EffectExpiredEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().EffectExpired);
services.AddSingleton<IEventBuffer<EffectTickedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().EffectTicked);
services.AddSingleton<IEventBuffer<AttackResolvedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().AttackResolved);
services.AddSingleton<IEventBuffer<EffectResolvedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().EffectResolved);
services.AddSingleton<IEventBuffer<AbilityUsedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().AbilityUsed);
services.AddSingleton<IEventBuffer<AbilityExecutedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().AbilityExecuted);
services.AddSingleton<IEventBuffer<ExperienceGrantedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().ExperienceGranted);
services.AddSingleton<IEventBuffer<LevelIncreasedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().LevelIncreased);
services.AddSingleton<IEventBuffer<KillRewardEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().KillReward);

// Core services
services.AddSingleton<IOutputService, OutputService>();
services.AddSingleton<ICommandBus, CommandBus>();
services.AddSingleton<IMessageBus, MessageBus>();
services.AddSingleton<IScheduler, Scheduler>();
services.AddSingleton<IActService, ActService>();
services.AddSingleton<IGameMessageService, GameMessageService>();
services.AddSingleton<IntentBusContainer>();
services.AddSingleton<IIntentContainer>(sp => sp.GetRequiredService<IntentBusContainer>());
services.AddSingleton<IIntentWriterContainer>(sp => sp.GetRequiredService<IntentBusContainer>());
services.AddSingleton<IConnectionService, ConnectionService>();

// Resolvers & factories (domain)
services.AddSingleton<IAbilityTargetResolver, AbilityTargetResolver>();
services.AddSingleton<IAggroResolver, AggroResolver>();
services.AddSingleton<IDamageResolver, DamageResolver>();
services.AddSingleton<IHealResolver, HealResolver>();
services.AddSingleton<IHitResolver, HitResolver>();
services.AddSingleton<IHitDamageFactory, HitDamageFactory>();
services.AddSingleton<IWeaponProcResolver, WeaponProcResolver>();
services.AddSingleton<IReactionResolver, ReactionResolver>();
services.AddSingleton<IEffectExecutor, EffectExecutor>();
services.AddSingleton<IEffectLifecycleManager, EffectLifecycleManager>();
services.AddSingleton<IEffectApplicationManager, EffectApplicationManager>();
services.AddSingleton<IExperienceService, ExperienceService>();
services.AddSingleton<ILookService, LookService>();

// Command dispatcher
services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

// Orchestrator
services.AddSingleton<ActionOrchestrator>();

// Systems
services.AddSingleton<CommandExecutionSystem>();
services.AddSingleton<CommandThrottleSystem>();
services.AddSingleton<FleeSystem>();
services.AddSingleton<MovementSystem>();
services.AddSingleton<ItemInteractionSystem>();
services.AddSingleton<EffectiveStatsSystem>();
services.AddSingleton(sp => new MaxResourcesSystem<BaseHealth, Health, DirtyHealth, HealthModifier>(x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new MaxResourcesSystem<BaseMana, Mana, DirtyMana, ManaModifier>(x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new MaxResourcesSystem<BaseEnergy, Energy, DirtyEnergy, EnergyModifier>(x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new MaxResourcesSystem<BaseRage, Rage, DirtyRage, RageModifier>(x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton<AbilityValidationSystem>();
services.AddSingleton<AbilityCastingSystem>();
services.AddSingleton<AbilityExecutionSystem>();
services.AddSingleton<AutoAttackSystem>();
services.AddSingleton<TimedEffectSystem>();
services.AddSingleton<ManaRegenSystem>();
services.AddSingleton<EnergyRegenSystem>();
services.AddSingleton<RageDecaySystem>();
services.AddSingleton<HealthRegenSystem>();
services.AddSingleton<ThreatDecaySystem>();
services.AddSingleton<ScheduleSystem>();
services.AddSingleton<DeathSystem>();
services.AddSingleton<RespawnSystem>();
services.AddSingleton<LootSystem>();
services.AddSingleton<LookSystem>();
services.AddSingleton<CleanupSystem>();

// Top-level
services.AddSingleton(new TelnetServer(port: 4000));
services.AddSingleton<GameLoop>();
services.AddSingleton<GameServer>();

// start game server
var sp = services.BuildServiceProvider();
sp.GetRequiredService<GameServer>().Start();
