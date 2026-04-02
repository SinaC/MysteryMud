using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Combat;
using MysteryMud.Domain.Combat.Factories;
using MysteryMud.Domain.Combat.Resolvers;
using MysteryMud.Domain.Damage.Resolvers;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Heal;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Eventing;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.ConsoleApp.Hosting;

internal class GameLoop
{
    private const int TickRateMs = 100;

    private readonly ILogger _logger;
    private readonly IOutputService _outputService;
    private readonly ICommandBus _commandBus;
    private readonly IMessageBus _messageBus;
    private readonly IScheduler _scheduler;
    private readonly IGameMessageService _gameMessageService;
    private readonly IIntentContainer _intentContainer;
    private readonly World _world;

    /*
     * public class EventBus
{
    private readonly Dictionary<Type, object> _buffers = new();

    public StructBuffer<T> GetBuffer<T>() where T : struct
    {
        if (!_buffers.TryGetValue(typeof(T), out var obj))
        {
            obj = new StructBuffer<T>(128);
            _buffers[typeof(T)] = obj;
        }

        return (StructBuffer<T>)obj;
    }

    public void ClearAll()
    {
        foreach (var buf in _buffers.Values)
        {
            var clearMethod = buf.GetType().GetMethod("Clear");
            clearMethod!.Invoke(buf, null);
        }
    }
}*/

    private readonly EventBuffer<FleeBlockedEvent> _fleeBlockedEventBuffer = new();
    private readonly EventBuffer<MovedEvent> _movedEventBuffer = new();
    private readonly EventBuffer<ItemGotEvent> _itemGotEventBuffer = new();
    private readonly EventBuffer<ItemDroppedEvent> _itemDroppedEventBuffer = new();
    private readonly EventBuffer<ItemGivenEvent> _itemGivenEventBuffer = new();
    private readonly EventBuffer<ItemPutEvent> _itemPutEventBuffer = new();
    private readonly EventBuffer<ItemWornEvent> _itemWornEventBuffer = new();
    private readonly EventBuffer<ItemRemovedEvent> _itemRemovedEventBuffer = new();
    private readonly EventBuffer<ItemDestroyedEvent> _itemDestroyedEventBuffer = new();
    private readonly EventBuffer<ItemSacrifiedEvent> _itemSacrifiedEventBuffer = new();
    private readonly EventBuffer<DamagedEvent> _damagedEventBuffer = new();
    private readonly EventBuffer<HealedEvent> _healedEventBuffer = new();
    private readonly EventBuffer<DeathEvent> _deathEventBuffer = new();
    private readonly EventBuffer<ItemLootedEvent> _itemLootedEventBuffer = new();
    private readonly EventBuffer<LookedEvent> _lookedEventBuffer = new();
    private readonly EventBuffer<TriggeredScheduledEvent> _triggeredScheduledEventBuffer = new();
    private readonly EventBuffer<EffectExpiredEvent> _effectExpiredEventBuffer = new();
    private readonly EventBuffer<EffectTickedEvent> _effectTickedEventBuffer = new();

    private readonly ILookService _lookService;

    private readonly AggroResolver _aggroResolver;
    private readonly DamageResolver _damageResolver;
    private readonly HealResolver _healResolver;
    private readonly HitResolver _hitResolver;
    private readonly HitDamageFactory _hitDamageFactory;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;
    private readonly CombatOrchestrator _combatOrchestrator;

    private readonly CommandExecutionSystem _commandExecutionSystem;
    private readonly CommandThrottleSystem _commandThrottleSystem;
    private readonly FleeSystem _fleeSystem;
    private readonly MovementSystem _movementSystem;
    private readonly ItemInteractionSystem _itemInteractionSystem;
    private readonly StatsSystem _statsSystem;
    private readonly AutoAttackSystem _autoAttackSystem;
    private readonly TimedEffectSystem _timedEffectSystem;
    private readonly ThreatDecaySystem _threatDecaySystem;
    private readonly ScheduleSystem _scheduleSystem;
    private readonly DeathSystem _deathSystem;
    private readonly RespawnSystem _respawnSystem;
    private readonly LootSystem _lootSystem;
    private readonly LookSystem _lookSystem;
    private readonly CleanupSystem _cleanupSystem;

    public GameLoop(ILogger logger, IOutputService putputService, ICommandBus commandBus, IMessageBus messageBus, IScheduler scheduler, IGameMessageService gameMessageService, IIntentContainer intentContainer, World world)
    {
        _logger = logger;
        _outputService = putputService;
        _commandBus = commandBus;
        _messageBus = messageBus;
        _scheduler = scheduler;
        _gameMessageService = gameMessageService;
        _intentContainer = intentContainer;
        _world = world;

        _lookService = new LookService(_gameMessageService);

        _aggroResolver = new AggroResolver();
        _damageResolver = new DamageResolver(_aggroResolver, _gameMessageService, _damagedEventBuffer, _deathEventBuffer);
        _healResolver = new HealResolver(_aggroResolver, _gameMessageService, _healedEventBuffer);
        _hitResolver = new HitResolver(_gameMessageService);
        _hitDamageFactory = new HitDamageFactory();
        _weaponProcResolver = new WeaponProcResolver();
        _reactionResolver = new ReactionResolver(_gameMessageService);
        _combatOrchestrator = new CombatOrchestrator(_gameMessageService, _intentContainer, _hitResolver, _hitDamageFactory, _damageResolver, _weaponProcResolver, _reactionResolver);

        _commandExecutionSystem = new CommandExecutionSystem();
        _commandThrottleSystem = new CommandThrottleSystem(_gameMessageService);
        _fleeSystem = new FleeSystem(_gameMessageService, _intentContainer, _fleeBlockedEventBuffer);
        _movementSystem = new MovementSystem(_gameMessageService, _intentContainer, _movedEventBuffer);
        _itemInteractionSystem = new ItemInteractionSystem(_gameMessageService, _intentContainer, _itemGotEventBuffer, _itemDroppedEventBuffer, _itemGivenEventBuffer, _itemPutEventBuffer, _itemWornEventBuffer, _itemRemovedEventBuffer, _itemDestroyedEventBuffer, _itemSacrifiedEventBuffer);
        _statsSystem = new StatsSystem();
        _autoAttackSystem = new AutoAttackSystem(_intentContainer);
        _timedEffectSystem = new TimedEffectSystem(_logger, _gameMessageService, _intentContainer, _damageResolver, _healResolver, _triggeredScheduledEventBuffer, _effectExpiredEventBuffer, _effectTickedEventBuffer);
        _threatDecaySystem = new ThreatDecaySystem();
        _scheduleSystem = new ScheduleSystem(scheduler, intentContainer);
        _deathSystem = new DeathSystem(_gameMessageService, _intentContainer, _deathEventBuffer);
        _respawnSystem = new RespawnSystem(_gameMessageService);
        _lootSystem = new LootSystem(_gameMessageService, _intentContainer, _itemLootedEventBuffer);
        _lookSystem = new LookSystem(_lookService, _intentContainer, _lookedEventBuffer);
        _cleanupSystem = new CleanupSystem(_logger);
    }

    public void Run()
    {
        _logger.LogInformation(LogEvents.System, "Starting game loop");

        var currentTick = 0;

        while (true)
        {
            CheckConsoleInput();

            Tick(currentTick);

            Thread.Sleep(TickRateMs); // tick rate

            currentTick++;
        }
    }

    private void Tick(int currentTick)
    {
        var state = new GameState
        {
            World = _world,
            CurrentTick = currentTick,
            CurrentTimeMs = currentTick * TickRateMs
        };

        var systemContext = new SystemContext
        {
            Log = _logger,
            Msg = _gameMessageService,
            Intent = _intentContainer,
        };

        // Player commands 
        _commandBus.Process(systemContext, state);
        // check spam, wait state, can cancel command
        _commandThrottleSystem.Execute(state);
        // execute command (if not cancelled) → may generate manual LookIntent(Mode= Snapshot)
        _commandExecutionSystem.Execute(systemContext, state);

        // TODO: stop invalid combat: dead entities, entities in different rooms, ...
        // Processes all LookIntents with Mode=Snapshot → reads current world state before any effects → produces messages
        _lookSystem.Tick(state, LookMode.Snapshot);
        // TODO: AISystem                         // NPC behavior → generates intents
        // Convert flee → MoveIntents
        _fleeSystem.Tick(state);
        // TODO: ChaseSystem                      // NPC chase movement
        // Resolve MoveIntents → emits auto-look PostUpdate (Mode=PostUpdate)
        _movementSystem.Tick(state);
        // Handle get/drop/put/give/...
        _itemInteractionSystem.Tick(state);
        // Recalculate stats from DirtyFlags
        _statsSystem.Tick(state);
        // Generate triggered scheduled event (tick or expired)
        _scheduler.Process(state, _triggeredScheduledEventBuffer);
        // Resolve triggered scheduled event and generates scheduleIntent (for next tick), effectExpiredEvent (to inform), effectTickedEvent (to inform)
        _timedEffectSystem.Tick(state);
        // Decay threat by 2%
        _threatDecaySystem.Tick(state);
        // TODO: NPCTargetSystem.AssignTargets   // Select highest threat targets
        // TODO: GroupCombatSystem.Resolve       // Handle assist/protect/own target attack intents
        // TODO: AbilitySystem                   // Resolve skill/spell usage → may generate DamageAction/HealAction/EffectActions
        // Generate AttackIntents for entities in combat
        _autoAttackSystem.Tick(state);
        // Resolve AttackIntents → AttackEvents + reactions, procs, spell effects, damage, heal, etc.
        _combatOrchestrator.Tick(state);
        // Flag dead entities
        _deathSystem.Tick(state);
        // Auto-resurrect players
        _respawnSystem.Tick(state);
        // Handle loot & auto-loot
        _lootSystem.Tick(state);
        // Processes LookIntents with Mode=PostUpdate → reflects final world state after all updates
        _lookSystem.Tick(state, LookMode.PostUpdate);
        // Handle scheduleIntents (which can be generated from IA, abilities, TimedEffectSytem, CombatOrchestrator)
        _scheduleSystem.Tick(state);

        // Remove destroyed items / dead NPCs / disconnected players
        _cleanupSystem.Tick(state);

        // Process messages to be sent to players
        _messageBus.Process(systemContext, state);

        // Send messages to players
        _outputService.FlushAll();

        // Clear intents of event buffers
        _intentContainer.ClearAll();
        _fleeBlockedEventBuffer.Clear();
        _movedEventBuffer.Clear();
        _itemGotEventBuffer.Clear();
        _itemDroppedEventBuffer.Clear();
        _itemGivenEventBuffer.Clear();
        _itemPutEventBuffer.Clear();
        _itemWornEventBuffer.Clear();
        _itemRemovedEventBuffer.Clear();
        _itemDestroyedEventBuffer.Clear();
        _itemSacrifiedEventBuffer.Clear();
        _damagedEventBuffer.Clear();
        _healedEventBuffer.Clear();
        _deathEventBuffer.Clear();
        _itemLootedEventBuffer.Clear();
        _lookedEventBuffer.Clear();
        _triggeredScheduledEventBuffer.Clear();
        _effectExpiredEventBuffer.Clear();
        _effectTickedEventBuffer.Clear();
    }

    /*
    private void Tick()
    {
        TimeSystem.NextTick();

        var state = new GameState
        {
            World = _world,
            CurrentTick = TimeSystem.CurrentTick
        };

        var systemContext = new SystemContext
        {
            Log = _logger,
            Msg = _gameMessageService,
            Scheduler = _scheduler,
            Intent = _intentContainer,
        };

        // process player commands
        _commandBus.Process(systemContext, state);

        // process scheduled events
        _scheduler.Process(systemContext, state);
        // handle state transitions for effects
        //StateMachineSystem.Update(world); TODO: implement state machine system and handle with scheduled events

        // AiSystem.Process(world);
        // handle combat rounds
        CombatSystem.Process(systemContext, state);

        // handle deaths and related consequences
        DeathSystem.Process(systemContext, state);

        // handle player deaths and respawns
        RespawnSystem.Process(systemContext, state);

        // recalculate stats for entities
        StatSystem.Process(state);

        // perform cleanup tasks like removing characters, items, ...
        CleanupSystem.Process(systemContext, state);

        // process messages to be sent to players
        _messageBus.Process(systemContext, state);

        // send output to players
        _outputService.FlushAll();
    }
    */

    private void CheckConsoleInput()
    {
        if (Console.KeyAvailable)
        {
            var line = Console.ReadLine();
            if (line != null)
            {
                switch (line)
                {
                    case "dump": DumpWorld(); break;
                        //TODO: case "shutdown": Shutdown(); break;
                        //world.Dispose();             // Clearing the world like God in the First Testament
                        //World.Destroy(world);        // Doomsday
                }
            }
        }
    }

    private void DumpWorld()
    {
        Console.WriteLine("Dumping world state:");
        var query = new QueryDescription();
        _world.Query(query, (Entity entity) =>
        {
            Console.WriteLine($"Entity Id: {entity.Id} Alive: {entity.IsAlive()} DebugName: {entity.DebugName}");
            Console.WriteLine($"  Components: {string.Join(", ", entity.GetAllComponents().Select(c => c?.GetType().Name))}");
        });
    }
}