using DefaultEcs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Commands.Commands.Admin;
using MysteryMud.Application.Commands.DataDrivenCommands;
using MysteryMud.Application.Commands.RegistryDependentCommands;
using MysteryMud.Application.Commands.RegistryDependentCommands.Admin;
using MysteryMud.Application.Dispatching;
using MysteryMud.Application.Registry;
using MysteryMud.Application.Services;
using MysteryMud.ConsoleApp;
using MysteryMud.ConsoleApp.Hosting;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Effects;
using MysteryMud.Core.Extensions;
using MysteryMud.Core.Persistence;
using MysteryMud.Core.Random;
using MysteryMud.Core.Scheduler;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Ability.Factories;
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
using MysteryMud.Domain.Action.Move;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Persistence;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Eventing;
using MysteryMud.Infrastructure.Intent;
using MysteryMud.Infrastructure.Network;
using MysteryMud.Infrastructure.Persistence;
using MysteryMud.Infrastructure.Persistence.Schema;
using MysteryMud.Infrastructure.Random;
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
var world = new World();

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
var admin = PlayerFactory.CreateAdmin(world, "admin", market);
var goblin = MobileFactory.CreateMob(world, "goblin", "a goblin", market);
var troll = MobileFactory.CreateMob(world, "troll", "a troll", market);
troll.Get<Level>().Value = 25;
troll.Get<Health>().Current = 1000;
troll.Get<Health>().Max = 1000;
troll.Get<BaseHealth>().Max = 1000;
ref var trollEffectiveStats = ref troll.Get<EffectiveStats>();
trollEffectiveStats.Dodge = 0; // for testing, make sure all hits land so we can see the counterattack in action
trollEffectiveStats.Parry = 0; // for testing, make sure all hits land so we can see the counterattack in action
trollEffectiveStats.CounterAttack = 100; // for testing, make sure all we counterattack every time so we can see the counterattack in action
var sword = ItemFactory.CreateItemInRoom(world, "sword", "a %#FFFFFF>#FFFF00shiny sword%x", market);
sword.Set(new Equipable { Slot = EquipmentSlotKind.MainHand });
sword.Set(new Weapon { Kind = WeaponKind.Sword, DiceCount = 5, DiceSides = 10, ProcIds = ["Flaming".ComputeUniqueId()] }); // add flaming
var chest = world.CreateEntity();
chest.Set(new ItemTag());
chest.Set(new Name { Value = "chest" });
chest.Set(new Description { Value = "a chest" });
chest.Set(new ItemEffects
{
    Data = new EffectsCollection
    {
        Effects = [],
        EffectsByTag = new List<Entity>?[32]
    },
});
chest.Set(new Location { Room = market });
chest.Set(new Container { Capacity = 10 });
chest.Set(new ContainerContents { Items = [] });
var dagger = ItemFactory.CreateItemInRoom(world, "dagger", "a vampiric dagger", market);
dagger.Set(new Equipable { Slot = EquipmentSlotKind.MainHand });
dagger.Set(new Weapon { Kind = WeaponKind.Dagger, DiceCount = 5, DiceSides = 8, ProcIds = ["Vampiric".ComputeUniqueId()] }); // add vampiric
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

var random = new SeededRandom();
var formulaCompiler = new EffectFormulaCompiler(random);

// load effect definitions
var effectLoader = new JsonEffectLoader(formulaCompiler);
var effectDefinitions = effectLoader.Load(Path.Combine(basePath, gamePaths.EffectsJson));
var effectActionFactory = new EffectActionFactory(logger);
var effectRuntimeFactory = new EffectRuntimeFactory(effectActionFactory);
var effectRegistry = new EffectRegistry(effectRuntimeFactory);
effectRegistry.Register(effectDefinitions);

// define ability outcome resolver registry
// TODO: autodiscover with reflection
var abilityOutcomeResolverRegistry = new AbilityOutcomeResolverRegistry();
abilityOutcomeResolverRegistry.Register("default", new DefaultOutcomeResolver());
abilityOutcomeResolverRegistry.Register("chancebased", new ChanceBasedOutcomeResolver(random));
abilityOutcomeResolverRegistry.Register("berserk", new BerserkOutcomeResolver(random));

// load ability definitions
var abilityLoader = new JsonAbilityLoader();
var abilityDefinitions = abilityLoader.Load(Path.Combine(basePath, gamePaths.AbilitiesJson));
var skillCommandDefinitions = abilityDefinitions.Where(x => x.Kind == AbilityKind.Skill && x.Command is not null).Select(x => x.Command!.Value).ToArray();
var validationRuleFactory = new ValidationRuleFactory(random);
var abilityRuntimeFactory = new AbilityRuntimeFactory(validationRuleFactory);
var abilityRegistry = new AbilityRegistry(effectRegistry, abilityOutcomeResolverRegistry, abilityRuntimeFactory);
abilityRegistry.Register(abilityDefinitions);

// load weapon proc definitions
var weaponProcLoader = new JsonWeaponProcLoader();
var weaponProcDefinitions = weaponProcLoader.Load(Path.Combine(basePath, gamePaths.WeaponProcsJson));
var weaponProcRegistry = new WeaponProcRegistry(effectRegistry);
weaponProcRegistry.Register(weaponProcDefinitions);

// load social definitions
var socialLoader = new JsonSocialLoader();
var socialDefinitions = socialLoader.Load(Path.Combine(basePath, gamePaths.SocialsJson));

// load command definitions
var commandLoader = new JsonCommandLoader();
var commandDefinitions = commandLoader.Load(Path.Combine(basePath, gamePaths.CommandsJson));

// run demo
//Demo.Run(logger, world, commandDispatcher);
//Demo2.Run(logger, world, commandDispatcher, effectRegistry);

var dbPath = Path.Combine(basePath, gamePaths.Db);
var connectionString = $"Data Source={dbPath};Pooling=True;";

// === DI container ===
var services = new ServiceCollection();

// Random
services.AddSingleton<IRandom, SeededRandom>();

// DB
// Run migration scripts
var migrationRunner = new MigrationRunner(connectionString, logger);
await migrationRunner.RunAsync();

// Wire persistence service
services.AddSingleton<IPersistenceService>(new SqlitePersistenceService(connectionString));

// Dirty tracker (singleton)
services.AddSingleton<IDirtyTracker, DirtyTracker>();

// Snapshot builder
services.AddSingleton<ISnapshotBuilder, PlayerSnapshotBuilder>();
services.AddSingleton<ISnapshotRestorer, PlayerSnapshotRestorer>();

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

// registry dependents and data-driven commands

// Social commands - data-driven
foreach (var socialDefinition in socialDefinitions)
    services.AddSingleton<IExplicitCommand>(sp => new SocialCommand(logger, sp.GetRequiredService<IGameMessageService>(), socialDefinition));

// Skill commands — data-driven
foreach (var skillCommandDefinition in skillCommandDefinitions)
    services.AddSingleton<IExplicitCommand>(sp => new SkillCommand(logger, sp.GetRequiredService<IAbilityRegistry>(), sp.GetRequiredService<IGameMessageService>(), sp.GetRequiredService<IIntentWriterContainer>(), skillCommandDefinition));


// RegisterCommands resolves all IExplicitCommand registrations
services.AddSingleton<CommandRegistry>();
services.AddSingleton<ICommandRegistry>(sp =>
{
    var registry = sp.GetRequiredService<CommandRegistry>();

    var explicitCommands = sp.GetServices<IExplicitCommand>().ToList(); // TestCommand, CastCommand, socials, skills

    // Add the ones that depend on ICommandRegistry manually — can't go through container
    explicitCommands.Add(new HelpCommand(registry, sp.GetRequiredService<IGameMessageService>()));
    explicitCommands.Add(new SocialsCommand(registry, sp.GetRequiredService<IGameMessageService>()));
    explicitCommands.Add(new ForceCommand(logger, registry, sp.GetRequiredService<IGameMessageService>()));
    explicitCommands.Add(new OrderCommand(logger, registry, sp.GetRequiredService<IGameMessageService>()));

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
services.AddSingleton<IEventBuffer<RoomEnteredEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().RoomEntered);
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
services.AddSingleton<IEventBuffer<AggressedEvent>>(sp => sp.GetRequiredService<EventBufferRegistry>().Aggressed);

// Core services
services.AddSingleton<ICommandBus, CommandBus>();
services.AddSingleton<IMessageBus, MessageBus>();
services.AddSingleton<IScheduler, Scheduler>();

// Infrastructure services
services.AddSingleton<IOutputService, OutputService>();
services.AddSingleton<IntentBusContainer>();
services.AddSingleton<IIntentContainer>(sp => sp.GetRequiredService<IntentBusContainer>());
services.AddSingleton<IIntentWriterContainer>(sp => sp.GetRequiredService<IntentBusContainer>());
services.AddSingleton<IConnectionService, ConnectionService>();

// Resolvers & factories & services (domain)
services.AddSingleton<IAbilityTargetResolver, AbilityTargetResolver>();
services.AddSingleton<IAggroResolver, AggroResolver>();
services.AddSingleton<IDamageResolver, DamageResolver>();
services.AddSingleton<IHealResolver, HealResolver>();
services.AddSingleton<IMoveResolver, MoveResolver>();
services.AddSingleton<IHitResolver, HitResolver>();
services.AddSingleton<IHitDamageFactory, HitDamageFactory>();
services.AddSingleton<IWeaponProcResolver, WeaponProcResolver>();
services.AddSingleton<IReactionResolver, ReactionResolver>();
services.AddSingleton<IEffectExecutor, EffectExecutor>();
services.AddSingleton<IEffectLifecycleManager, EffectLifecycleManager>();
services.AddSingleton<IEffectApplicationManager, EffectApplicationManager>();
services.AddSingleton<IExperienceService, ExperienceService>();
services.AddSingleton<IActService, ActService>();
services.AddSingleton<IGameMessageService, GameMessageService>();
services.AddSingleton<ILookService, LookService>();
services.AddSingleton<ISacrificeService, SacrificeService>();
services.AddSingleton<IEffectDisplayService, EffectDisplayService>();
services.AddSingleton<IFollowService, FollowService>();
services.AddSingleton<IGroupService, GroupService>();
services.AddSingleton<ICastMessageService, CastMessageService>();
services.AddSingleton<ICombatService, CombatService>();

// Command dispatcher
services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

// Orchestrator
services.AddSingleton<ActionOrchestrator>();

// Systems
services.AddSingleton<CommandExecutionSystem>();
services.AddSingleton<CommandThrottleSystem>();
services.AddSingleton<AutoAssistSystem>();
services.AddSingleton<FleeSystem>();
services.AddSingleton<MovementSystem>();
services.AddSingleton<FollowSystem>();
services.AddSingleton<ItemInteractionSystem>();
services.AddSingleton<EffectiveCharacterStatsSystem>();
services.AddSingleton(sp => new EffectiveMaxResourceSystem<BaseHealth, Health, DirtyHealth, HealthModifier>(world, x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveMaxResourceSystem<BaseMove, Move, DirtyMove, MoveModifier>(world, x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveMaxResourceSystem<BaseMana, Mana, DirtyMana, ManaModifier>(world, x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveMaxResourceSystem<BaseEnergy, Energy, DirtyEnergy, EnergyModifier>(world, x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveMaxResourceSystem<BaseRage, Rage, DirtyRage, RageModifier>(world, x => x.Max, x => x.Current, (ref x, v) => x.Current = v, (ref x, v) => x.Max = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveResourceRegenSystem<HealthRegen, DirtyHealthRegen, HealthRegenModifier>(world, x => x.BaseAmountPerSecond, (ref x, v) => x.CurrentAmountPerSecond = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveResourceRegenSystem<MoveRegen, DirtyMoveRegen, MoveRegenModifier>(world, x => x.BaseAmountPerSecond, (ref x, v) => x.CurrentAmountPerSecond = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveResourceRegenSystem<ManaRegen, DirtyManaRegen, ManaRegenModifier>(world, x => x.BaseAmountPerSecond, (ref x, v) => x.CurrentAmountPerSecond = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveResourceRegenSystem<EnergyRegen, DirtyEnergyRegen, EnergyRegenModifier>(world, x => x.BaseAmountPerSecond, (ref x, v) => x.CurrentAmountPerSecond = v, x => x.Modifier, x => x.Value));
services.AddSingleton(sp => new EffectiveResourceRegenSystem<RageDecay, DirtyRageDecay, RageDecayModifier>(world, x => x.BaseAmountPerSecond, (ref x, v) => x.CurrentAmountPerSecond = v, x => x.Modifier, x => x.Value));
services.AddSingleton<AbilityValidationSystem>();
services.AddSingleton<AbilityCastingSystem>();
services.AddSingleton<AbilityExecutionSystem>();
services.AddSingleton<AggressionSystem>();
services.AddSingleton<AutoAttackSystem>();
services.AddSingleton<TimedEffectSystem>();
services.AddSingleton(sp => new ResourceRegenSystem<Health, HealthRegen>(world, x => x.Current, x => x.Max, x => x.CurrentAmountPerSecond, (ref x, v) => x.Current = v));
services.AddSingleton(sp => new ResourceRegenSystem<Move, MoveRegen>(world, x => x.Current, x => x.Max, x => x.CurrentAmountPerSecond, (ref x, v) => x.Current = v));
services.AddSingleton(sp => new ResourceRegenSystem<Mana, ManaRegen>(world, x => x.Current, x => x.Max, x => x.CurrentAmountPerSecond, (ref x, v) => x.Current = v));
services.AddSingleton(sp => new ResourceRegenSystem<Energy, EnergyRegen>(world, x => x.Current, x => x.Max, x => x.CurrentAmountPerSecond, (ref x, v) => x.Current = v));
services.AddSingleton(sp => new ResourceRegenSystem<Rage, RageDecay>(world, x => x.Current, x => x.Max, x => -x.CurrentAmountPerSecond, (ref x, v) => x.Current = v));
services.AddSingleton<ThreatDecaySystem>();
services.AddSingleton<ScheduleSystem>();
services.AddSingleton<DeathSystem>();
services.AddSingleton<RespawnSystem>();
services.AddSingleton<LootSystem>();
services.AddSingleton<AutoSacrificeSystem>();
services.AddSingleton<LookSystem>();
services.AddSingleton<DisconnectSystem>();
services.AddSingleton<PersistenceSystem>(); // TODO: use options for change AutosaveInternal and ImmediateFlushThreshold
services.AddSingleton<CleanupSystem>();

// Top-level
services.AddSingleton(new TelnetServer(port: 4000));
services.AddSingleton<GameLoop>();
services.AddSingleton<GameServer>();

// start game server
var sp = services.BuildServiceProvider();
sp.GetRequiredService<GameServer>().Start();
