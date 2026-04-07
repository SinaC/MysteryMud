using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Action;
using MysteryMud.Domain.Combat.Attack.Factories;
using MysteryMud.Domain.Combat.Attack.Resolvers;
using MysteryMud.Domain.Combat.Damage;
using MysteryMud.Domain.Combat.Effect;
using MysteryMud.Domain.Combat.Effect.Factories;
using MysteryMud.Domain.Combat.Heal;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Eventing;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.ConsoleApp.Hosting;

internal class GameLoop
{
    private const int TickRateMs = 100;
    private const int TickRegenRate = 10;

    private readonly ILogger _logger;
    private readonly IOutputService _outputService;
    private readonly ICommandBus _commandBus;
    private readonly IMessageBus _messageBus;
    private readonly IScheduler _scheduler;
    private readonly IGameMessageService _gameMessageService;
    private readonly IIntentContainer _intentContainer;
    private readonly EffectRegistry _effectRegistry;
    private readonly AbilityRegistry _abilityRegistry;
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
    // TODO: deathEvent and damageEvent are purely combat events and should probably remains the only event passed to systems, other events like itemGotEvent, itemDroppedEvent, itemGivenEvent, itemPutEvent can be directly sent to message service without going through event buffer since they are only used for messaging and no system needs to react to them, this way we can avoid the complexity of managing multiple event buffers and also avoid the issue of events being processed in the wrong order (like damage events being processed before attack intents)
    // TODO: we should replace all these eventbuffers with a more generic event system

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
    private readonly EventBuffer<AttackResolvedEvent> _attackResolvedEventBuffer = new();
    private readonly EventBuffer<EffectResolvedEvent> _effectResolvedEventBuffer = new();
    private readonly EventBuffer<AbilityUsedEvent> _abilityUsedEventBuffer = new();
    private readonly EventBuffer<AbilityExecutedEvent> _abilityExecutedEventBuffer = new();

    private readonly ILookService _lookService;

    private readonly AggroResolver _aggroResolver;
    private readonly DamageResolver _damageResolver;
    private readonly HealResolver _healResolver;
    private readonly HitResolver _hitResolver;
    private readonly HitDamageFactory _hitDamageFactory;
    private readonly WeaponProcResolver _weaponProcResolver;
    private readonly ReactionResolver _reactionResolver;

    private readonly EffectFactory _effectFactory;

    private readonly ActionOrchestrator _actionOrchestrator;

    private readonly CommandExecutionSystem _commandExecutionSystem;
    private readonly CommandThrottleSystem _commandThrottleSystem;
    private readonly FleeSystem _fleeSystem;
    private readonly MovementSystem _movementSystem;
    private readonly ItemInteractionSystem _itemInteractionSystem;
    private readonly StatsSystem _statsSystem;
    private readonly AbilityValidationSystem _abilityValidationSystem;
    private readonly AbilityCastingSystem _abilityCastingSystem;
    private readonly AbilityExecutionSystem _abilityExecutionSystem;
    private readonly AutoAttackSystem _autoAttackSystem;
    private readonly TimedEffectSystem _timedEffectSystem;
    private readonly ManaRegenSystem _manaRegenSystem;
    private readonly EnergyRegenSystem _energyRegenSystem;
    private readonly RageDecaySystem _rageDecaySystem;
    private readonly HealthRegenSystem _healthRegenSystem;
    private readonly ThreatDecaySystem _threatDecaySystem;
    private readonly ScheduleSystem _scheduleSystem;
    private readonly DeathSystem _deathSystem;
    private readonly RespawnSystem _respawnSystem;
    private readonly LootSystem _lootSystem;
    private readonly LookSystem _lookSystem;
    private readonly CleanupSystem _cleanupSystem;

    public GameLoop(ILogger logger, IOutputService putputService, ICommandBus commandBus, IMessageBus messageBus, IScheduler scheduler, IGameMessageService gameMessageService, IIntentContainer intentContainer, EffectRegistry effectRegistry, AbilityRegistry abilityRegistry, World world)
    {
        _logger = logger;
        _outputService = putputService;
        _commandBus = commandBus;
        _messageBus = messageBus;
        _scheduler = scheduler;
        _gameMessageService = gameMessageService;
        _intentContainer = intentContainer;
        _effectRegistry = effectRegistry;
        _abilityRegistry = abilityRegistry;
        _world = world;

        _lookService = new LookService(_gameMessageService);

        _aggroResolver = new AggroResolver();
        _damageResolver = new DamageResolver(_aggroResolver, _gameMessageService, _damagedEventBuffer, _deathEventBuffer);
        _healResolver = new HealResolver(_aggroResolver, _gameMessageService, _healedEventBuffer);
        _hitResolver = new HitResolver(_gameMessageService);
        _hitDamageFactory = new HitDamageFactory();
        _weaponProcResolver = new WeaponProcResolver(_logger, _gameMessageService, _intentContainer, _effectRegistry);
        _reactionResolver = new ReactionResolver(_gameMessageService);

        _effectFactory = new EffectFactory(_logger, _gameMessageService, _intentContainer, _damageResolver, _healResolver);

        _actionOrchestrator = new ActionOrchestrator(_logger, _intentContainer, _attackResolvedEventBuffer, _effectResolvedEventBuffer, _effectRegistry, _effectFactory, _hitResolver, _hitDamageFactory, _damageResolver, _weaponProcResolver, _reactionResolver);

        _commandExecutionSystem = new CommandExecutionSystem(_logger);
        _commandThrottleSystem = new CommandThrottleSystem(_gameMessageService);
        _fleeSystem = new FleeSystem(_gameMessageService, _intentContainer, _fleeBlockedEventBuffer);
        _movementSystem = new MovementSystem(_gameMessageService, _intentContainer, _movedEventBuffer);
        _itemInteractionSystem = new ItemInteractionSystem(_gameMessageService, _intentContainer, _itemGotEventBuffer, _itemDroppedEventBuffer, _itemGivenEventBuffer, _itemPutEventBuffer, _itemWornEventBuffer, _itemRemovedEventBuffer, _itemDestroyedEventBuffer, _itemSacrifiedEventBuffer);
        _statsSystem = new StatsSystem();
        _abilityValidationSystem = new AbilityValidationSystem(_logger, _gameMessageService, _intentContainer, _abilityRegistry);
        _abilityCastingSystem = new AbilityCastingSystem(_logger, _gameMessageService, _intentContainer, _abilityRegistry);
        _abilityExecutionSystem = new AbilityExecutionSystem(_logger, _intentContainer, _abilityExecutedEventBuffer, _abilityRegistry, _effectRegistry);
        _autoAttackSystem = new AutoAttackSystem(_intentContainer);
        _timedEffectSystem = new TimedEffectSystem(_logger, _gameMessageService, _intentContainer, _damageResolver, _healResolver, _triggeredScheduledEventBuffer, _effectExpiredEventBuffer, _effectTickedEventBuffer);
        _manaRegenSystem = new ManaRegenSystem();
        _energyRegenSystem = new EnergyRegenSystem();
        _rageDecaySystem = new RageDecaySystem();
        _healthRegenSystem = new HealthRegenSystem();
        _threatDecaySystem = new ThreatDecaySystem();
        _scheduleSystem = new ScheduleSystem(_logger, _scheduler, _intentContainer);
        _deathSystem = new DeathSystem(_gameMessageService, _intentContainer, _deathEventBuffer);
        _respawnSystem = new RespawnSystem(_gameMessageService);
        _lootSystem = new LootSystem(_gameMessageService, _intentContainer, _itemLootedEventBuffer);
        _lookSystem = new LookSystem(_lookService, _intentContainer, _lookedEventBuffer);
        _cleanupSystem = new CleanupSystem(_logger, _effectFactory);
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
        //_logger.LogDebug("TICK: {currentTick}", currentTick);
        using (_logger.BeginScope(new Dictionary<string, object> { ["TICK"] = currentTick }))
        {
            var state = new GameState
            {
                World = _world,
                CurrentTick = currentTick,
                CurrentTimeMs = currentTick * TickRateMs
            };

            var executionContext = new CommandExecutionContext
            {
                Msg = _gameMessageService,
                Intent = _intentContainer,
            };

            // Player commands 
            _commandBus.Process(executionContext, state);
            // check spam, wait state, can cancel command
            _commandThrottleSystem.Execute(state);
            // execute command (if not cancelled) → may generate manual LookIntent(Mode= Snapshot)
            _commandExecutionSystem.Execute(executionContext, state);
            // TODO: stop invalid combat: dead entities, entities in different rooms, ...
            // Process all LookIntents with Mode=Snapshot → reads current world state before any effects → produces messages
            _lookSystem.Tick(state, LookMode.Snapshot);
            // TODO: AISystem                         // NPC behavior → generates intents
            // Convert flee → MoveIntents
            _fleeSystem.Tick(state);
            // TODO: ChaseSystem                      // NPC chase movement
            // Process MoveIntents → emits auto-look PostUpdate (Mode=PostUpdate)
            _movementSystem.Tick(state);
            // Process get/drop/put/give/...
            _itemInteractionSystem.Tick(state);
            // Recalculate stats from DirtyFlags
            _statsSystem.Tick(state);
            // Generate triggered scheduled event (tick or expired)
            _scheduler.Process(state, _triggeredScheduledEventBuffer);
            // Resolve triggered scheduled event and generates scheduleIntent (for next tick), effectExpiredEvent (to inform), effectTickedEvent (to inform)
            _timedEffectSystem.Tick(state);
            // Regen
            if (state.CurrentTimeMs % TickRegenRate == 0)
            {
                _manaRegenSystem.Tick(state);
                _energyRegenSystem.Tick(state);
                _rageDecaySystem.Tick(state);
                _healthRegenSystem.Tick(state);
            }
            // Decay threat by 2%
            _threatDecaySystem.Tick(state);
            // TODO: NPCTargetSystem.AssignTargets   // Select highest threat targets
            // TODO: GroupCombatSystem.Resolve       // Handle assist/protect/own target attack intents
            // Process UseAbilityIntents -> set casting (if delayed casting) or generate ExecuteAbilityIntent (instant cast)
            _abilityValidationSystem.Tick(state);
            // Process delayed casting, once cast is effective generate ExecuteAbilityIntent + abilityUsedEvent
            _abilityCastingSystem.Tick(state);
            // Process ExecuteAbilityIntents -> generate ActionIntent(kind:effect) for each effects in ability + abilityExecutedEvent
            _abilityExecutionSystem.Tick(state);
            // Generate AttackIntents for entities in combat
            _autoAttackSystem.Tick(state);
            // Process ActionIntents. kind:attack -> resolve hit, perform damage, check weapon proc (effect), check reaction (counter attack)  kind: effect -> resolve effect
            _actionOrchestrator.Tick(state);
            // Process dead entities: remove from combat, remove casting, create corpse -> generate loot intent
            _deathSystem.Tick(state);
            // Auto-resurrect players
            _respawnSystem.Tick(state);
            // Handle loot & auto-loot
            _lootSystem.Tick(state);
            // Process LookIntents with Mode=PostUpdate → reflects final world state after all updates
            _lookSystem.Tick(state, LookMode.PostUpdate);
            // Handle scheduleIntents (which can be generated from IA, abilities, TimedEffectSytem, AttackOrchestrator)
            _scheduleSystem.Tick(state);

            // Remove destroyed items / dead NPCs / disconnected players
            _cleanupSystem.Tick(state);

            // Process messages to be sent to players
            _messageBus.Process(state);

            // Send messages to players
            _outputService.FlushAll();

            // Clear intents
            _intentContainer.ClearAll();
            // Clear event buffers
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
            _attackResolvedEventBuffer.Clear();
            _effectResolvedEventBuffer.Clear();
            _abilityUsedEventBuffer.Clear();
            _abilityExecutedEventBuffer.Clear();
        }
    }

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